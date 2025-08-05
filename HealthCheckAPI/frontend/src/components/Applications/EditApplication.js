import React, { useState, useEffect } from 'react';
import './EditApplications.css';

function EditApplication({ app, onUpdate, onCancel }) {
    const [editHealthCheckUrl, setEditHealthCheckUrl] = useState('');
    const [editConnectionString, setEditConnectionString] = useState('');
    const [editQuery, setEditQuery] = useState('');
    const [editMessage, setEditMessage] = useState('');
    const [successMessage, setSuccessMessage] = useState("");


    const token = localStorage.getItem('token');

    useEffect(() => {
        if (app) {
            if (app.type === 'WebApp') {
                setEditHealthCheckUrl(app.healthCheckUrl || '');
            } else if (app.type === 'Database') {
                setEditConnectionString(app.connectionString || '');
                setEditQuery(app.query || '');
            }
        }
    }, [app]);

    const handleEditApplication = async (e) => {
        e.preventDefault();

        if (!token) {
            setEditMessage('❌ Δεν είστε συνδεδεμένος.');
            return;
        }


        const payload = {
            id: app.id,
            name: app.name,
            type: app.type,
            healthCheckUrl: app.type === 'WebApp' ? editHealthCheckUrl.trim() : '',
            connectionString: app.type === 'Database' ? editConnectionString.trim() : '',
            query: app.type === 'Database' ? editQuery.trim() : '',
        };

        try {
            const response = await fetch(`https://localhost:7057/api/Application/${app.id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`,
                },
                body: JSON.stringify(payload),
            });

            if (response.status === 200 || response.status === 204) {
                setEditMessage('✅ Application updated successfully!');
                onUpdate(payload);
                setTimeout(() => setEditMessage(''), 3000);
            } else {
                const errorText = await response.text();
                setEditMessage(`❌ Failed to update: ${errorText}`);
            }
        } catch (error) {
            setEditMessage('❌ Error updating application: ' + error.message);
        }

        setTimeout(() => setEditMessage(''), 3000);
    };

    if (!app) return null;

    return (
        <div className="edit-app-container">
            <h2>Edit Application</h2>
            <form className="app-form" onSubmit={handleEditApplication}>
                <label>ID: <input type="text" value={app.id} disabled /></label>
                <label>Name: <input type="text" value={app.name} disabled /></label>
                <label>Type: <input type="text" value={app.type} disabled /></label>

                {app.type === 'WebApp' && (
                    <label>Health Check URL:
                        <input
                            type="text"
                            value={editHealthCheckUrl}
                            onChange={(e) => setEditHealthCheckUrl(e.target.value)}
                            required
                        />
                    </label>
                )}

                {app.type === 'Database' && (
                    <>
                        <label>Connection String:
                            <input
                                type="text"
                                value={editConnectionString}
                                onChange={(e) => setEditConnectionString(e.target.value)}
                                required
                            />
                        </label>
                        <label>Query:
                            <input
                                type="text"
                                value={editQuery}
                                onChange={(e) => setEditQuery(e.target.value)}
                                required
                            />
                        </label>
                    </>
                )}

                <button type="submit" className="submit-btn">Save Changes</button>
                <button type="button" className="cancel-btn" onClick={onCancel}>Cancel</button>
            </form>

            {editMessage && (
                <p className={`message ${editMessage.startsWith('✅') ? 'success' : 'error'}`}>
                    {editMessage}
                </p>
            )}
        </div>
    );
}

export default EditApplication;
