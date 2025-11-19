// Passkey Login JavaScript
(function() {
    'use strict';

    // Check if WebAuthn is supported
    const isWebAuthnSupported = () => {
        return !!(
            window.PublicKeyCredential &&
            navigator.credentials &&
            typeof navigator.credentials.create === 'function' &&
            typeof navigator.credentials.get === 'function'
        );
    };

    // Hide passkey button if not supported
    if (!isWebAuthnSupported()) {
        const passkeyButton = document.getElementById('passkey-button');
        if (passkeyButton) {
            passkeyButton.style.display = 'none';
            const divider = document.querySelector('.divider');
            if (divider) divider.style.display = 'none';
        }
        return;
    }

    // Base64 URL encoding/decoding helpers
    const base64UrlDecode = (input) => {
        input = input.replace(/-/g, '+').replace(/_/g, '/');
        const pad = input.length % 4;
        if (pad) {
            if (pad === 1) {
                throw new Error('Invalid base64url string');
            }
            input += new Array(5 - pad).join('=');
        }
        return Uint8Array.from(atob(input), c => c.charCodeAt(0));
    };

    const base64UrlEncode = (buffer) => {
        const binary = String.fromCharCode(...new Uint8Array(buffer));
        return btoa(binary)
            .replace(/\+/g, '-')
            .replace(/\//g, '_')
            .replace(/=/g, '');
    };

    const base64Encode = (buffer) => {
        const binary = String.fromCharCode(...new Uint8Array(buffer));
        return btoa(binary);
    };

    // Show/hide loading state
    const showLoading = (show) => {
        const form = document.getElementById('login-form');
        const loading = document.getElementById('loading');
        if (show) {
            form.style.display = 'none';
            loading.style.display = 'block';
        } else {
            form.style.display = 'block';
            loading.style.display = 'none';
        }
    };

    // Show error message
    const showError = (message) => {
        const validationSummary = document.querySelector('.validation-summary');
        if (validationSummary) {
            validationSummary.style.display = 'block';
            validationSummary.innerHTML = `<div class="error-message">${message}</div>`;
        } else {
            alert(message);
        }
    };

    // Handle passkey login
    const handlePasskeyLogin = async () => {
        try {
            showLoading(true);

            // Get email from the form
            const emailInput = document.querySelector('input[name="Input.Email"]');
            const email = emailInput?.value?.trim();

            if (!email) {
                showError('Please enter your email address.');
                showLoading(false);
                return;
            }

            // Step 1: Begin authentication - get challenge from server
            const beginResponse = await fetch('/passkey/authenticate/begin', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ username: email })
            });

            if (!beginResponse.ok) {
                const error = await beginResponse.json();
                throw new Error(error.message || 'Failed to initiate passkey authentication');
            }

            const options = await beginResponse.json();

            // Step 2: Convert base64url strings to ArrayBuffers
            const credentialRequestOptions = {
                publicKey: {
                    challenge: base64UrlDecode(options.challenge),
                    timeout: options.timeout || 60000,
                    rpId: options.rpId,
                    allowCredentials: options.allowCredentials?.map(cred => ({
                        type: 'public-key',
                        id: base64UrlDecode(cred.id),
                        transports: cred.transports
                    })) || [],
                    userVerification: options.userVerification || 'preferred'
                }
            };

            // Step 3: Call WebAuthn API
            const credential = await navigator.credentials.get(credentialRequestOptions);

            if (!credential) {
                throw new Error('No credential returned from authenticator');
            }

            // Step 4: Prepare assertion for server
            // Note: rawId uses base64url, but response fields use standard base64
            const assertion = {
                id: credential.id,
                rawId: base64UrlEncode(credential.rawId),
                type: credential.type,
                response: {
                    clientDataJSON: base64Encode(credential.response.clientDataJSON),
                    authenticatorData: base64Encode(credential.response.authenticatorData),
                    signature: base64Encode(credential.response.signature),
                    userHandle: credential.response.userHandle ?
                        base64UrlEncode(credential.response.userHandle) : null
                }
            };

            // Step 5: Send assertion to server for validation
            const completeResponse = await fetch('/passkey/authenticate/complete', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    username: email,
                    assertion: assertion
                })
            });

            if (!completeResponse.ok) {
                const error = await completeResponse.json();
                throw new Error(error.message || 'Failed to validate passkey');
            }

            const result = await completeResponse.json();

            // Step 6: Authentication successful - redirect to return URL
            const urlParams = new URLSearchParams(window.location.search);
            const returnUrl = urlParams.get('ReturnUrl') || urlParams.get('returnUrl') || '/';
            window.location.href = returnUrl;

        } catch (error) {
            console.error('Passkey login error:', error);

            let errorMessage = 'An error occurred during passkey authentication.';

            if (error.name === 'NotAllowedError') {
                errorMessage = 'Authentication was cancelled or timed out.';
            } else if (error.name === 'InvalidStateError') {
                errorMessage = 'This passkey has already been registered.';
            } else if (error.name === 'NotSupportedError') {
                errorMessage = 'Passkeys are not supported on this device.';
            } else if (error.message) {
                errorMessage = error.message;
            }

            showError(errorMessage);
            showLoading(false);
        }
    };

    // Attach event listener
    const passkeyButton = document.getElementById('passkey-button');
    if (passkeyButton) {
        passkeyButton.addEventListener('click', handlePasskeyLogin);
    }
})();
