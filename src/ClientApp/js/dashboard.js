const token = localStorage.getItem('bankToken');
const userName = localStorage.getItem('bankUser');

// Se não tiver token, manda pro login
if (!token) window.location.href = 'index.html';

document.getElementById('userNameDisplay').innerText = userName || "Astronaut";

// Função melhorada para extrair o ID do Token (cobre vários padrões do C#)
function getAccountIdFromToken(token) {
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload.AccountId || payload.nameid || payload.sub;
    } catch (e) { return null; }
}

const myAccountId = getAccountIdFromToken(token);

// Se conseguiu pegar o ID, mostra na tela
if (myAccountId) {
    document.getElementById('userIdDisplay').innerText = `#${myAccountId}`;
} else {
    document.getElementById('userIdDisplay').innerText = "ID Error";
}

document.getElementById('logoutBtn').addEventListener('click', () => {
    localStorage.clear();
    window.location.href = 'index.html';
});

// Atualiza o Saldo com proteção Anti-Zumbi (401)
async function refreshBalance() {
    try {
        const response = await fetch(`http://localhost:5000/api/account/${myAccountId}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        // SE O TOKEN ESTIVER EXPIRADO OU INVÁLIDO, DESLOGA AUTOMATICAMENTE
        if (response.status === 401) {
            localStorage.clear();
            window.location.href = 'index.html';
            return;
        }

        if (response.ok) {
            const data = await response.json();
            document.getElementById('balanceDisplay').innerText = `$ ${data.balance.toFixed(2)}`;
        }
    } catch (err) { console.error("Failed to fetch balance"); }
}

// Atualiza o Extrato com proteção Anti-Zumbi (401)
async function loadTransactions() {
    try {
        const response = await fetch(`http://localhost:5000/api/account/${myAccountId}/transactions`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.status === 401) return; // Se der 401, o refreshBalance já vai deslogar

        if (response.ok) {
            const list = await response.json();
            const container = document.getElementById('transactionList');
            
            if (list.length === 0) {
                container.innerHTML = '<p style="color: var(--text-muted);">No recent activity...</p>';
                return;
            }

            container.innerHTML = list.map(t => {
                const safeType = (t.type || '').toLowerCase();
                const safeDesc = t.description || 'Processing...';
                
                const isIncome = safeType.includes('in') || safeType.includes('deposit');
                const colorCode = isIncome ? '#2ecc71' : '#ff4757';
                const sign = isIncome ? '+' : '-';
                
                return `
                <div style="padding: 12px; border-left: 4px solid ${colorCode}; background: #1E293B; border-radius: 6px; display: flex; justify-content: space-between; align-items: center;">
                    <div>
                        <small style="color: var(--text-muted); text-transform: uppercase; font-size: 0.7rem;">${t.type}</small>
                        <p style="margin: 4px 0 0 0; color: white; font-size: 0.9rem;">${safeDesc}</p>
                    </div>
                    <strong style="color: ${colorCode}; white-space: nowrap; margin-left: 15px;">${sign} $ ${t.amount.toFixed(2)}</strong>
                </div>
                `;
            }).join('');
        }
    } catch (e) { console.log("Error loading history"); }
}

function notify(text, isError = true) {
    const el = document.getElementById('statusMessage');
    el.style.display = 'block';
    el.style.color = isError ? "#ff4757" : "#2ecc71";
    el.innerText = text;
    setTimeout(() => { el.style.display = 'none'; }, 5000); 
}

// Deposit Logic
document.getElementById('depositForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const amount = parseFloat(document.getElementById('depAmount').value);
    const btn = e.target.querySelector('button');
    btn.innerText = "Processing...";

    try {
        const response = await fetch(`http://localhost:5000/api/account/${myAccountId}/deposit`, {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ Amount: amount })
        });

        if (response.status === 401) {
            notify("Session expired. Please login again.");
            return;
        }

        if (response.ok) {
            notify("Funds added successfully!", false);
            refreshBalance();
            loadTransactions();
            e.target.reset();
        } else {
            const err = await response.text();
            notify(err);
        }
    } catch (err) { notify("Connection error."); }
    finally { btn.innerText = "Add Funds"; }
});

// Transfer Logic
document.getElementById('transferForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const amount = parseFloat(document.getElementById('transAmount').value);
    const receiverId = parseInt(document.getElementById('receiverId').value);
    const btn = e.target.querySelector('button');
    btn.innerText = "Processing...";

    try {
        const response = await fetch('http://localhost:5000/api/account/transfer', {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ SenderId: parseInt(myAccountId), ReceiverId: receiverId, Amount: amount })
        });

        if (response.ok) {
            notify("Transfer successful!", false);
            refreshBalance();
            loadTransactions();
            e.target.reset();
        } else {
            const err = await response.text();
            notify(err);
        }
    } catch (err) { notify("Connection error."); }
    finally { btn.innerText = "Send Money"; }
});

// Iniciadores
refreshBalance();
loadTransactions();

// Loop a cada 5 segundos
setInterval(refreshBalance, 5000);
setInterval(loadTransactions, 5000);