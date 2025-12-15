# Firebase Authentication with SignalR Integration

This document describes the Firebase authentication integration with SignalR hub connections implemented in this project. This implementation ensures secure, authenticated real-time communication by automatically managing Firebase ID tokens and tying SignalR hub lifecycle to user authentication state, providing seamless user experience and robust security.

## Overview

The application uses Firebase for user authentication and automatically manages SignalR hub connections based on authentication state. The SignalR hub is secured with Firebase ID tokens that are automatically refreshed and injected into the connection.

## Architecture

### Components

1. **firebase.js** - Core Firebase configuration and token management
2. **authService.js** - Authentication service for login/logout operations
3. **signalRService.js** - SignalR connection management with auth integration
4. **tokenStorage.js** - Secure token storage with obfuscation
5. **eventBus.js** - Event system for auth lifecycle communication

### Authentication Lifecycle Events

Three key events coordinate authentication across the application:

- `auth:signed-in` - Triggered when user successfully authenticates
- `auth:signed-out` - Triggered when user logs out
- `auth:requires-relogin` - Triggered when token is invalid or revoked

## Key Features

### 1. Token Management (`firebase.js`)

#### `getFreshIdToken()`
Returns a fresh Firebase ID token, automatically refreshing if it expires within 5 minutes.

```javascript
import { getFreshIdToken } from './firebase'

const token = await getFreshIdToken()
```

**Behavior:**
- Checks token expiration time
- Proactively refreshes if ≤ 5 minutes remaining
- Handles revocation/invalid user errors
- Emits `auth:requires-relogin` on critical errors

#### `signOutAll()`
Performs complete sign-out with cleanup:

```javascript
import { signOutAll } from './firebase'

await signOutAll()
```

**Actions:**
- Clears all tokens from localStorage and sessionStorage
- Signs out from Firebase
- Emits `auth:signed-out` event
- Triggers SignalR disconnection

### 2. SignalR Hub Integration

#### Automatic Connection Management

SignalR connections are automatically managed based on auth state:

```javascript
// Connection starts automatically when user signs in
// Connection stops automatically when user signs out or relogin required
```

#### Token Injection

Firebase ID tokens are automatically injected into SignalR connections:

```javascript
// In signalRService.js
.withUrl(hubUrl, {
  accessTokenFactory: () => this.tokenFactory()
})
```

The token factory:
- Is called during initial negotiate
- Is called on every reconnect
- Returns fresh tokens (auto-refreshed if needed)

#### 401/403 Error Handling

The service implements robust error handling:

1. On auth error (401/403):
   - Attempts token refresh
   - Retries connection once
   - Signs out if retry fails

2. On token revocation:
   - Emits `auth:requires-relogin`
   - Stops hub connection
   - Redirects to login

### 3. Token Storage

Tokens are stored with basic obfuscation (not cryptographic encryption):

```javascript
import { storeToken, getToken, removeToken } from './service/tokenStorage'

// Store token
storeToken('FirebaseToken', token, rememberMe)

// Retrieve token
const token = getToken('FirebaseToken', rememberMe)

// Remove token
removeToken('FirebaseToken')
```

**Security Notes:**
- Uses XOR-based obfuscation (not secure encryption)
- Obfuscation prevents casual token inspection in browser DevTools
- Better than plaintext storage, but not cryptographically secure
- Real security relies on HTTPS and backend validation
- Prevents casual token inspection
- Supports both localStorage and sessionStorage

## Configuration

### Environment Variables

Create a `.env` file based on `.env.example`:

```env
# SignalR Configuration
VITE_SIGNALR_BASE_URL=http://localhost:8446
VITE_SIGNALR_HUB_PATH=/signalr

# API Configuration
VITE_API_BASE_URL=http://localhost:5049/api

# Environment mode
VITE_ENV=QA  # or PROD
```

### Backend Requirements

The backend must:

1. **Validate Firebase ID tokens** on both:
   - WebAPI endpoints
   - SignalR hub connections

2. **Accept token in Authorization header**:
   ```
   Authorization: Bearer <firebase-id-token>
   ```

3. **Return 401/403** on invalid tokens to trigger refresh flow

## Usage Examples

### Login Flow

```javascript
// In Login.vue
import { loginWithGoogle, notifySignIn } from '../service/authService'
import { storeToken } from '../service/tokenStorage'

// 1. Authenticate with Firebase
const result = await loginWithGoogle()
const FirebaseToken = await result.user.getIdToken()

// 2. Validate with backend
const apiResponse = await authApiService.login(FirebaseToken, rememberMe)

// 3. Store tokens securely
storeToken('FirebaseToken', FirebaseToken, rememberMe)
storeToken('AccessToken', apiResponse.AccessToken, rememberMe)

// 4. Notify system of sign-in (starts SignalR)
notifySignIn()

// 5. Navigate to app
router.push({ name: 'Project' })
```

### Logout Flow

```javascript
// In TopBar.vue
import { logout } from '../service/authService'

// Logout (cleans up everything automatically)
await logout()
// - Clears all tokens
// - Signs out from Firebase
// - Emits auth:signed-out (stops SignalR)
// - Redirects to login
```

### Manual Token Refresh

```javascript
import { getFreshIdToken } from './firebase'

try {
  const freshToken = await getFreshIdToken()
  // Use token for API call
} catch (error) {
  // Token refresh failed, user will be prompted to relogin
}
```

## Error Handling

### Token Expiration

Tokens are proactively refreshed before expiration:
- Threshold: 5 minutes before expiry
- Automatic refresh on next `getFreshIdToken()` call
- No user interruption

### Token Revocation

When Firebase reports token revocation:
1. `auth:requires-relogin` event is emitted
2. SignalR hub is disconnected
3. User is redirected to login page
4. All tokens are cleared

### Network Errors

SignalR handles network errors with:
- Automatic reconnection (exponential backoff)
- Token refresh on reconnect
- Status updates via `SIGNALR_STATUS_CHANGED` event

### Clock Skew

The 5-minute early-refresh threshold handles:
- Minor clock differences between client/server
- Token validation timing issues
- Race conditions during negotiate

## Testing

### Manual Testing Checklist

1. **Login Flow**
   - [ ] Login with Google/Microsoft/Facebook
   - [ ] Verify SignalR connects after login
   - [ ] Check tokens stored in browser storage

2. **Token Refresh**
   - [ ] Wait for token to approach expiration
   - [ ] Verify automatic refresh occurs
   - [ ] Check SignalR remains connected

3. **Logout Flow**
   - [ ] Click logout
   - [ ] Verify SignalR disconnects
   - [ ] Check all tokens cleared
   - [ ] Confirm redirect to login

4. **Error Scenarios**
   - [ ] Simulate 401 error from backend
   - [ ] Verify token refresh attempt
   - [ ] Check user redirected to login on failure

5. **Reconnection**
   - [ ] Disconnect network
   - [ ] Reconnect network
   - [ ] Verify SignalR reconnects with fresh token

## Security Considerations

### Client-Side

- **No Sensitive Data in Logs**: Tokens are never logged
- **Token Obfuscation**: Basic protection against casual inspection
- **Automatic Cleanup**: Tokens cleared on logout/error

### Server-Side Required

The backend MUST:
- Validate Firebase ID tokens on every request
- Check token expiration
- Verify token signature with Firebase
- Handle revoked tokens appropriately
- Use HTTPS in production

### Best Practices

1. **Never expose tokens**: Don't log or transmit tokens in URLs
2. **Short token lifetime**: Firebase ID tokens expire in 1 hour
3. **Validate on every request**: Backend must check every token
4. **Use HTTPS**: Always use HTTPS in production
5. **Regular token refresh**: Proactive refresh prevents mid-request failures

## Troubleshooting

### SignalR Won't Connect

1. Check if user is authenticated:
   ```javascript
   import { auth } from './firebase'
   console.log('Current user:', auth.currentUser)
   ```

2. Verify environment variables:
   ```javascript
   console.log('Hub URL:', import.meta.env.VITE_SIGNALR_BASE_URL)
   ```

3. Check browser console for errors

### Token Refresh Fails

1. Check Firebase Auth state:
   ```javascript
   const user = auth.currentUser
   if (user) {
     const token = await user.getIdToken(true) // Force refresh
   }
   ```

2. Verify user hasn't been disabled/deleted in Firebase Console

### Connection Keeps Dropping

1. Check network connectivity
2. Verify backend is running on correct port
3. Check backend logs for authentication errors
4. Ensure CORS is configured if needed

## Migration Notes

### From Previous Implementation

If upgrading from manual SignalR initialization:

1. **Remove manual `initialize()` calls**:
   ```javascript
   // OLD - Remove this
   await signalRService.initialize('/editorHub')
   
   // NEW - Automatic via auth lifecycle
   // No code needed
   ```

2. **Update logout calls**:
   ```javascript
   // OLD
   localStorage.removeItem('AccessToken')
   await signOut(auth)
   
   // NEW
   import { logout } from './service/authService'
   await logout()
   ```

3. **Add environment variables**:
   - Create `.env` file
   - Set `VITE_SIGNALR_BASE_URL` and `VITE_SIGNALR_HUB_PATH`

## Future Enhancements

Potential improvements (ordered by priority):

**High Priority:**
- [ ] Add refresh token rotation (Complexity: Medium)
- [ ] Multi-tab token synchronization (Complexity: Medium)

**Medium Priority:**
- [ ] Implement token caching strategy (Complexity: Low)
- [ ] Enhanced encryption for token storage (Complexity: Medium)
- [ ] Token usage metrics/monitoring (Complexity: Medium)

**Low Priority:**
- [ ] Add offline token validation (Complexity: High)
