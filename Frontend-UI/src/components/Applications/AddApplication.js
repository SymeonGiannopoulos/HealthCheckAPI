import React, { useState } from 'react';
import { toast, ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';


function AddApplication({ onAdd }) {
    const [name, setName] = useState('');
    const [type, setType] = useState('');
    const [healthCheckUrl, setHealthCheckUrl] = useState('');
    const [connectionString, setConnectionString] = useState('');
    const [query, setQuery] = useState('');
    const [message, setMessage] = useState('');

    const token = localStorage.getItem('token');

    const handleAddApplication = async (e) => {
        e.preventDefault();

        if (!token) {
            toast.error('Δεν είστε συνδεδεμένος.');
            return;
        }

        const newApp = {
            name: name.trim(),
            type,
            healthCheckUrl: type === 'WebApp' ? healthCheckUrl.trim() : '',
            connectionString: type === 'Database' ? connectionString.trim() : '',
            query: type === 'Database' ? query.trim() : ''
        };

        try {
            const response = await fetch('https://localhost:7057/api/Application', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify(newApp)
            });

            if (response.ok) {
                const addedApp = await response.json();
                toast.success('Application added successfully!');
                onAdd(addedApp);
                setName('');
                setType('');
                setHealthCheckUrl('');
                setConnectionString('');
                setQuery('');
            } else {
                const errorText = await response.text();
                toast.error('Error adding application: ' + errorText);
            }
        } catch (error) {
            toast.error('Request failed: ' + error.message);
        }
    };


    return (
        <div>
            <h3 style={{ textAlign: 'center' }}>Προσθήκη νέας εφαρμογής</h3>
            <form className="app-form" onSubmit={handleAddApplication}>
                <label>Name:
                    <input type="text" value={name} onChange={e => setName(e.target.value)} required />
                </label>
                <label>Type:
                    <select value={type} onChange={e => setType(e.target.value)} required>
                        <option value="">-- Select Type --</option>
                        <option value="WebApp">WebApp</option>
                        <option value="Database">Database</option>
                    </select>
                </label>
                {type === 'WebApp' && (
                    <label>Health Check URL:
                        <input type="text" value={healthCheckUrl} onChange={e => setHealthCheckUrl(e.target.value)} required />
                    </label>
                )}
                {type === 'Database' && (
                    <>
                        <label>Connection String:
                            <input type="text" value={connectionString} onChange={e => setConnectionString(e.target.value)} required />
                        </label>
                        <label>Query:
                            <input type="text" value={query} onChange={e => setQuery(e.target.value)} required />
                        </label>
                    </>
                )}
                <button type="submit" className="submit-btn">Προσθήκη</button>
            </form>
            {message && <p className={`message ${message.startsWith('✅') ? 'success' : 'error'}`}>{message}</p>}
        </div>
    );
}

export default AddApplication;
