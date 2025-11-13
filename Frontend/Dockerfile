# Stage 1: Build static assets
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .

# 👇 Declare and use build args
ARG VITE_FIREBASE_SHIT
ENV VITE_FIREBASE_SHIT=${VITE_FIREBASE_SHIT}

# Optional: check value (for debug only)
# RUN echo "Building with VITE_FIREBASE_SHIT=$VITE_FIREBASE_SHIT"

# Build the application
RUN npm run build

# Stage 2: Serve with Nginx
FROM nginx:alpine

# Optional: custom config
# COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built files
COPY --from=build /app/dist /usr/share/nginx/html

# 👇 Add this line if you created nginx/default.conf
COPY nginx/default.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

