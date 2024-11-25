# IdentityCore Project

## Overview

**IdentityCore** is a robust authentication and authorization service designed for secure user registration, login, and
account management using **JWT** tokens. It supports authentication via **Email & Password** and **Google 3rd Party
Login**, providing a modern, stateless approach to identity management.

Key features include:

- Secure password storage with **SHA-512** and **Base64** encoded salts.
- Stateless authentication with **JWT** and **Refresh Tokens**.
- Integration with **PostgreSQL** for persistent storage.
- **Redis** caching with strict **TTL** policies.
- Asynchronous email notifications via [**NotificationService**](https://github.com/Isukaza/NotificationService) and *
  *RabbitMQ**.

---

## Features

### Registration

- **Email & Password**:
    - Requires email verification for account activation.
- **Google Login**:
    - Bypasses email confirmation. The application requests access to the user’s email and username.

### Authentication

- On successful login, users receive:
    - **JWT Token** for secure API access.
    - **Refresh Token** for renewing the JWT upon expiration.
- Stateless architecture with **no cookies** used for session management.

---

## Security

### Password Management

Passwords are securely stored using a multi-step hashing process:

1. Generate a 64-bit random salt and encode it in Base64.
2. Combine the plaintext password with the salt and hash it using **SHA-512**.
3. Store the hash and salt in the database.

This ensures strong resistance to brute-force and dictionary attacks.

---

## Data Management

### Database

**PostgreSQL** serves as the primary storage for user and session information, consisting of two main tables:

1. **Users**: Stores user account details, including:
    - `GUID`: Unique user identifier.
    - `Username`: User's display name.
    - `Email`: User's email address.
    - `Password` and `Salt`: Securely hashed password and corresponding salt.
    - `isActive`: Account activation status (whether the user has activated their account).
    - `RegistrationProvider`: Identifies the method of user registration:
        - `Local`: Registered using email and password.
        - `Google`: Registered using Google login.
        - `GoogleWithPass`: Initially registered via email, later linked to a Google account.
    - `Created`: Timestamp for when the user account was created.
    - `Modified`: Timestamp for the last modification of the user account.

2. **RefreshTokens**: Stores refresh tokens used for JWT renewal, with the following attributes:
    - `Id`: Unique identifier for the refresh token.
    - `RefToken`: The actual refresh token string.
    - `Expires`: Expiry timestamp for the refresh token.
    - `UserId`: Foreign key linking the refresh token to the corresponding user (references the **Users** table).
    - `Created`: Timestamp for when the refresh token was created.
    - `Modified`: Timestamp for the last modification of the refresh token record.

These two tables, **Users** and **RefreshTokens**, form the backbone of the data storage for user accounts and their
associated session management in **IdentityCore**. The **Users** table holds essential information for user
authentication, while the **RefreshTokens** table enables the handling of secure, stateless JWT authentication.

### Caching

**Redis** is employed for caching frequently accessed data with:

- **TTL-based expiration** to manage cache lifecycle.
- No password protection, as Redis operates within a secured and isolated server environment.

---

## Account Management

### Update Mechanisms

1. **Update Request**:
    - Users can request updates to their own accounts.
    - Changes require confirmation via email.
2. **Direct Update**:
    - Available to **Super Admin** and **Admin** roles.
    - Changes are applied immediately without email confirmation.
    - Admins have privileges to update any user account.

---

## Notifications

Notifications are managed through the [**NotificationService**](https://github.com/Isukaza/NotificationService):

- Messages are published to a shared **RabbitMQ** queue.
- **NotificationService** processes these messages and sends email notifications to users.

> **Important**: Ensure that **IdentityCore** and **NotificationService** are connected to the same RabbitMQ instance to enable smooth operation.

---

## Technology Stack

- **Authentication**: JWT (Access Tokens)
- **Database**: PostgreSQL
    - **Version**: 17.0
- **Caching**: Redis
    - **Version**: 7.4.1
- **Messaging**: RabbitMQ
    - **Version**: 4.0-management
- **Notifications**: NotificationService
    - **Version**: Latest

---

## IdentityCore Project Deployment with Docker Compose

### Overview

This document outlines the deployment of the **IdentityCore** project using Docker Compose, specifically configured for
local development with **Traefik** as a reverse proxy.

### Docker Compose Configuration

Below is the Docker Compose configuration for the **IdentityCore** service:

```yaml
version: '3.8'

services:
  identitycore:
    image: isukaza/identity_core:latest  # Use this image for testing
    container_name: identitycore
    networks:
      - soundify
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: "Production"
      ASPNETCORE_URLS: "http://+:80"
      JWT_KEY: ${JWT_KEY}
      GOOGLE_AUTH_ClientSecret: ${GOOGLE_AUTH_ClientSecret}
      RABBITMQ_USER: ${RABBITMQ_USER}
      RABBITMQ_PASS: ${RABBITMQ_PASS}
      POSTGRES_USER_IC: ${POSTGRES_USER_IC}
      POSTGRES_PASSWORD_IC: ${POSTGRES_PASSWORD_IC}
    volumes:
      - "{local-path-to-settings}:/app/appsettings.json"  # Replace with actual path
    labels:
      traefik.enable: "true"
      traefik.http.routers.identitycore.rule: "Host(localhost) && PathPrefix(/ic)"
      traefik.http.routers.identitycore.entrypoints: "websecure"
      traefik.http.routers.identitycore.tls.certresolver: "myresolver"
      traefik.http.services.identitycore.loadbalancer.server.port: "80"
      traefik.http.middlewares.identitycore-strip.stripprefix.prefixes: "/ic"
      traefik.http.routers.identitycore.middlewares: "identitycore-strip"

networks:
  soundify:
    driver: bridge
```

### Building the Docker Image

To build the Docker image for **IdentityCore**, use the following command:

```bash
  docker build -t identity_core:latest -f IdentityCore/Dockerfile .
```

---

## Deployment and Environment Setup

### Docker-Oriented Design
**IdentityCore** is designed to operate seamlessly within a Docker environment. The service offers two operational environments:

1. **Development**:
    - Runs on HTTPS using a self-signed SSL certificate.
    - Suitable for local testing and development.

2. **Production**:
    - Operates on HTTP, expecting proxied secure traffic from an external **reverse proxy** (e.g., NGINX or Traefik).

---

### Prerequisites

Before starting, ensure the following dependencies are installed and configured:

1. **PostgreSQL**: For storing user and session data.
2. **Redis**: For caching.
3. **RabbitMQ**: For message queueing.
4. **NotificationService**: For handling email delivery.

### Configuration

For **Production** mode, define the following environment variables in your `.env` file:

- `JWT_KEY`: Secret key used to sign JWT tokens.
- `GOOGLE_AUTH_ClientSecret`: Google OAuth client secret for Google login integration.
- `RABBITMQ_USER`: RabbitMQ username.
- `RABBITMQ_PASS`: RabbitMQ password.
- `POSTGRES_USER_IC`: PostgreSQL username for IdentityCore.
- `POSTGRES_PASSWORD_IC`: PostgreSQL password for IdentityCore.

**Important Note**: Ensure that all special characters in the values are URL encoded to prevent configuration errors due
to incompatible characters.

For **Development** mode, you can use the default `appsettings.Development.json` file. In **Production** mode, it's
recommended to mount the `appsettings.json` file as a volume to easily manage and configure the settings of the
container without modifying the Docker image directly.

---

### Running the Service

1. Start the service in Docker using the appropriate configuration for your environment (Development or Production).
2. Verify connectivity with:
    - PostgreSQL for database operations.
    - Redis for caching.
    - RabbitMQ for messaging.
    - NotificationService for email delivery.

3. Test the key workflows:
    - Registration (Email & Google).
    - Login.
    - Account updates and email notifications.

---

## Swagger Documentation in Development Mode

In **Development mode**, **Swagger** is available for easier API exploration and testing. It provides:

- Examples of expected request and response data.
- XML documentation for each **endpoint** of the service.
- An interactive interface for sending API requests directly.

You can access the Swagger documentation at the following URL:

[https://localhost:7433/api/index.html](https://localhost:7433/api/index.html)

---

## Future Enhancements

For information on the project's future plans and enhancements, please refer to the [Roadmap](https://github.com/Isukaza/IdentityCore/blob/develop/ROADMAP.md).

---

**IdentityCore** is continuously evolving to meet modern authentication needs. Planned enhancements include additional
authentication methods, advanced role-based access control, and integration with external APIs.