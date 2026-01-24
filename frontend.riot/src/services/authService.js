// src/services/authService.js
import { auth, googleProvider, facebookProvider, microsoftProvider, signOutAll as firebaseSignOutAll } from '../firebase'
import {
    createUserWithEmailAndPassword,
    signInWithEmailAndPassword,
    signInWithPopup
} from 'firebase/auth'
import { triggerEvent, EVENT_TYPES } from '../rete/eventBus'

export function registerWithEmail(email, pw) {
    return createUserWithEmailAndPassword(auth, email, pw)
}

export function loginWithEmail(email, pw) {
    return signInWithEmailAndPassword(auth, email, pw)
}

export function loginWithGoogle() {
    return signInWithPopup(auth, googleProvider)
}

export function loginWithFacebook() {
    return signInWithPopup(auth, facebookProvider)
}

export function loginWithMicrosoft() {
    return signInWithPopup(auth, microsoftProvider)
}

/**
 * Logout and clean up all sessions
 */
export async function logout() {
    try {
        await firebaseSignOutAll()
        // Event is emitted by signOutAll function
    } catch (error) {
        console.error('Logout error:', error)
        throw error
    }
}

/**
 * Emit sign-in event after successful authentication
 * Should be called after Firebase authentication completes
 */
export function notifySignIn() {
    triggerEvent(EVENT_TYPES.AUTH_SIGNED_IN)
}