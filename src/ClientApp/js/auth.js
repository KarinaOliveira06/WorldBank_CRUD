
const loginSection = document.getElementById('loginSection');
const registerSection = document.getElementById('registerSection');
const messageDiv = document.getElementById('message');


document.getElementById('showLogin').addEventListener('click', () => {
    loginSection.style.display = 'block';
    registerSection.style.display = 'none';
    messageDiv.style.display = 'none';
});

document.getElementById('showRegister').addEventListener('click', () => {
    loginSection.style.display = 'none';
    registerSection.style.display = 'block';
    messageDiv.style.display = 'none';
});

document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = e.target.querySelector('button');
    const originalText = btn.innerText;
    btn.innerText = "Processing...";
    
    const body = {
        AccountNumber: parseInt(document.getElementById('loginAccount').value),
        Name: document.getElementById('loginName').value,
        Password: document.getElementById('loginPassword').value
    };
    await callApi('login', body, btn, originalText);
});

document.getElementById('registerForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = e.target.querySelector('button');
    const originalText = btn.innerText;
    btn.innerText = "Processing...";

    const body = {
        Name: document.getElementById('regName').value,
        AccountNumber: parseInt(document.getElementById('regAccount').value),
        Password: document.getElementById('regPassword').value,
        Balance: 1000.0,
        Role: "User"
    };
    await callApi('register', body, btn, originalText);
});

async function callApi(endpoint, body, btn, originalText) {
    messageDiv.style.display = 'none';
    
    try {
        const response = await fetch(`http://localhost:5000/api/account/${endpoint}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        if (!response.ok) {
            const errorText = await response.text(); // Lê como texto puro
            messageDiv.style.color = "#ff4757";
            
            try {
                const errorJson = JSON.parse(errorText);
                messageDiv.innerText = errorJson.message || "Invalid credentials.";
            } catch {
                messageDiv.innerText = errorText || "Invalid credentials.";
            }
            
            messageDiv.style.display = 'block';
            btn.innerText = originalText;
            return;
        }

        const data = await response.json();

        if (endpoint === 'login') {
            localStorage.setItem('bankToken', data.token);
            localStorage.setItem('bankUser', data.user);
            window.location.href = 'dashboard.html';
        } else {
            messageDiv.style.color = "#2ecc71";
            messageDiv.innerText = "Registration complete! You can now login.";
            messageDiv.style.display = 'block';
            btn.innerText = originalText;
            document.getElementById('registerForm').reset();
        }
    } catch (err) {
        console.error("Fetch error:", err);
        messageDiv.style.color = "#ff4757";
        messageDiv.innerText = "API Offline. Verifique se o Docker está rodando na porta 5000.";
        messageDiv.style.display = 'block';
        btn.innerText = originalText;
    }
}