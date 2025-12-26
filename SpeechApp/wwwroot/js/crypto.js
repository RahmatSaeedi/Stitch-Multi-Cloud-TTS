// Web Crypto API wrapper for encryption/decryption

window.cryptoHelper = {
    // Derive encryption key from password using PBKDF2
    async deriveKey(password, salt) {
        const enc = new TextEncoder();
        const keyMaterial = await window.crypto.subtle.importKey(
            "raw",
            enc.encode(password),
            "PBKDF2",
            false,
            ["deriveBits", "deriveKey"]
        );

        return await window.crypto.subtle.deriveKey(
            {
                name: "PBKDF2",
                salt: salt,
                iterations: 100000,
                hash: "SHA-256"
            },
            keyMaterial,
            { name: "AES-GCM", length: 256 },
            true,
            ["encrypt", "decrypt"]
        );
    },

    // Encrypt plaintext using AES-GCM
    async encrypt(plaintext, password, salt) {
        try {
            const enc = new TextEncoder();
            const key = await this.deriveKey(password, salt);

            // Generate random IV
            const iv = window.crypto.getRandomValues(new Uint8Array(12));

            // Encrypt
            const encrypted = await window.crypto.subtle.encrypt(
                {
                    name: "AES-GCM",
                    iv: iv
                },
                key,
                enc.encode(plaintext)
            );

            // Combine IV + encrypted data
            const combined = new Uint8Array(iv.length + encrypted.byteLength);
            combined.set(iv, 0);
            combined.set(new Uint8Array(encrypted), iv.length);

            // Return as base64
            return btoa(String.fromCharCode(...combined));
        } catch (error) {
            console.error('Encryption error:', error);
            throw error;
        }
    },

    // Decrypt ciphertext using AES-GCM
    async decrypt(ciphertext, password, salt) {
        try {
            const dec = new TextDecoder();
            const key = await this.deriveKey(password, salt);

            // Decode from base64
            const combined = Uint8Array.from(atob(ciphertext), c => c.charCodeAt(0));

            // Extract IV and encrypted data
            const iv = combined.slice(0, 12);
            const encrypted = combined.slice(12);

            // Decrypt
            const decrypted = await window.crypto.subtle.decrypt(
                {
                    name: "AES-GCM",
                    iv: iv
                },
                key,
                encrypted
            );

            return dec.decode(decrypted);
        } catch (error) {
            console.error('Decryption error:', error);
            throw error;
        }
    },

    // Generate random salt
    generateSalt() {
        const salt = window.crypto.getRandomValues(new Uint8Array(16));
        return btoa(String.fromCharCode(...salt));
    },

    // Hash password for validation (SHA-256)
    async hashPassword(password, salt) {
        const enc = new TextEncoder();
        const data = enc.encode(password + salt);
        const hashBuffer = await window.crypto.subtle.digest('SHA-256', data);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        return btoa(String.fromCharCode(...hashArray));
    }
};
