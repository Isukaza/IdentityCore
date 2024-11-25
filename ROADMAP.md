# **IdentityCore Roadmap**

This document outlines the planned features, enhancements, and improvements for the **IdentityCore** project. The goal is to continuously improve the authentication and authorization service to meet modern security standards while ensuring scalability, reliability, and maintainability.

---

## **Planned Features and Enhancements**

### **1. Transactional Session Updates with Database Locks (Transaction Isolation Levels)**

**Objective**: Ensure atomicity and consistency when updating user sessions and account data.

- **Implementation**:
    - Use **transaction isolation levels** (e.g., **Serializable**) to prevent race conditions when updating user and session data.
    - Apply database-level locks to ensure that updates to both **Users** and **RefreshTokens** tables occur atomically, preventing conflicts during concurrent updates.
    - Updates will be performed within a single transaction to maintain data integrity and avoid partial updates during session-based operations.

- **Expected Outcome**:
    - Enhanced consistency and safety for user data updates.
    - Prevention of race conditions when handling session-based operations (e.g., login, password change).

---

### **2. Centralized RabbitMQ Producer via Background Task**

**Objective**: Improve message queue handling by centralizing the message publishing process and implementing a reliable background task.

- **Implementation**:
    - Extract the RabbitMQ message producer logic into a separate **background task**.
    - The task will handle message publishing to RabbitMQ in a centralized manner to prevent redundant code and ensure proper error handling.
    - Configure a **message queue** with retry and dead-letter mechanisms to ensure that messages are sent even during intermittent RabbitMQ connection issues.

- **Expected Outcome**:
    - Improved reliability and resilience when interacting with RabbitMQ.
    - Centralized task management for sending notifications (email, system alerts).
    - Better message queue management, ensuring messages are not lost during transient failures.

---

### **3. Password Hashing Algorithm Improvement: Migration to Argon2**

**Objective**: Upgrade the password hashing mechanism to Argon2 for improved security.

- **Implementation**:
    - Migrate from **SHA-512** to **Argon2**, a modern and recommended password hashing algorithm.
    - Argon2 provides better resistance to brute-force attacks and is designed to be computationally expensive, making it ideal for password security.
    - Implement configuration options to allow **customized parameters** (e.g., memory cost, iterations) for Argon2.
    - Ensure backward compatibility with existing users by implementing a migration strategy for stored password hashes.

- **Expected Outcome**:
    - Enhanced password security through a stronger hashing algorithm.
    - Improved resistance to modern attack vectors such as GPU-accelerated cracking.

---

### **4. JWT Blacklist for Instant Token Revocation**

**Objective**: Enable immediate revocation of JWT tokens to enhance security in the event of user account bans, suspensions, logouts, or other security incidents.

- **Implementation**:
    - Implement a **JWT blacklist** in **Redis** to store and check tokens against a list of revoked tokens.
    - When an account is banned, suspended, or a user logs out, add the corresponding JWT to the blacklist, invalidating the token before its expiration.
    - Blacklisted JWT tokens will be stored in Redis with an expiration time equal to the TTL (Time-To-Live) of the JWT. Once the JWT expires, it will be automatically removed from the blacklist in Redis.
    - Periodically review and optimize Redis storage to ensure efficient use of resources and performance.

- **Expected Outcome**:
    - Immediate invalidation of JWT tokens in cases such as account bans, password changes, security breaches, or user logouts.
    - Enhanced control over active sessions and better management of token lifecycles.
    - Efficient storage and retrieval of blacklisted tokens using Redis, improving performance and scalability.
    - Improved security for token management and user account safety.

---

### **5. Ban List for User Accounts**

**Objective**: Track and manage banned user accounts, providing a mechanism to handle user suspensions effectively.

- **Implementation**:
    - Add a **ban list** to the **Users** table or a separate table, where banned users are tracked.
    - Integrate with the **JWT blacklist** so that when an account is banned, the user's tokens are invalidated, and access is revoked.
    - Implement a **reason and timestamp** field to record why and when the user was banned.

- **Expected Outcome**:
    - Improved management of banned accounts.
    - Centralized tracking of bans, including reasons and timestamps.
    - Automated revocation of access upon account suspension.

---

### **6. "Log Out from All Sessions" Feature**

**Objective**: Allow users to log out from all sessions at once, providing better control over active sessions.

- **Implementation**:
    - Create a feature that invalidates all **JWT tokens** and **refresh tokens** associated with a user's account.
    - Provide an **API endpoint** that allows users to request this action securely.
    - Ensure that all active sessions are logged out by invalidating tokens stored in **Redis** and the **RefreshTokens** table.

- **Expected Outcome**:
    - Enhanced security for users who wish to log out from all sessions at once.
    - Useful in cases like device theft, account compromise, or simply wanting to reset session access.

---

### **7. Google reCAPTCHA Integration**

**Objective**: Improve security by protecting against automated attacks (bots) during operations like registration, login, and password changes.

- **Implementation**:
    - Integrate **Google reCAPTCHA** on the login, registration, and password reset pages.
    - Support **reCAPTCHA v3** for passive user activity checks without additional interaction, or **reCAPTCHA v2** for explicit user verification.
    - Implement server-side validation for reCAPTCHA responses to ensure correctness.

- **Expected Outcome**:
    - Enhanced protection against bot attacks.
    - Improved security for operations like user registration and login.
    - Reduced number of fake registrations and brute force attempts.

---

### **8. Rate Limiting (Request Count Control)**

**Objective**: Protect the system from excessive requests from a single user or IP address, preventing Denial of Service (DoS) attacks and reducing server load.

- **Implementation**:
    - Implement **rate limiting** for API and critical endpoints (e.g., registration, login, password reset).
    - Use **Redis** or other solutions to store request counts for each user/IP.
    - Set **time windows** (e.g., 100 requests within 10 minutes), after which requests from the same user/IP will be blocked or throttled.
    - Provide flexible settings for different types of operations (e.g., stricter limits for login and registration, more lenient for informational requests).

- **Expected Outcome**:
    - Protection from brute force attacks.
    - Improved system performance and stability by limiting server load.
    - Enhanced security by preventing the use of bots and automated scripts.

---

## **Future Improvements**

### **9. Multi-Region and Multi-Tenant Support**

- **Objective**: Scale the system to support multi-region and multi-tenant deployments.
- **Implementation**: Allow **IdentityCore** to serve multiple regions or customers while keeping data isolated for each tenant.

---

## **Development and Deployment Milestones**

### **Q1 2025:**

- Complete migration to **Argon2** for password hashing.
- Implement the **JWT blacklist** system.
- Release the **ban list** and **log out from all sessions** features.
- Complete **Google reCAPTCHA** integration.
- Implement **rate limiting**.

### **Q2 2025:**

- Centralize **RabbitMQ producer** in background task.
- Implement **transactional session updates** with database locking.

### **Q3 2025:**

- Finalize support for **multi-region** and **multi-tenant** deployments.
- Release **Swagger UI** enhancements for better API documentation.

---

## **Conclusion**

The **IdentityCore** project is continuously evolving to provide robust, scalable, and secure authentication and authorization services. By focusing on key areas like password security, token management, session control, and protections against automated attacks, the roadmap will ensure that **IdentityCore** remains a cutting-edge solution for modern identity management.

We are committed to delivering these enhancements while maintaining high standards of security, reliability, and performance.

---
