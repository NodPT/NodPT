# Stage 1: Build static assets
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies using the project files inside src/
COPY src/package*.json ./
RUN npm ci

# Copy the rest of the frontend source code
COPY src/ ./

# 👇 Declare and use build args
ARG VITE_FIREBASE_SHIT
ENV VITE_FIREBASE_SHIT=${VITE_FIREBASE_SHIT}

# Build the application
RUN npm run build

# Stage 2: Serve with Nginx
FROM nginx:alpine

# Copy built files from the build stage
COPY --from=build /app/dist /usr/share/nginx/html

# 👇 Add this line if you created nginx/default.conf
COPY src/nginx/default.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
