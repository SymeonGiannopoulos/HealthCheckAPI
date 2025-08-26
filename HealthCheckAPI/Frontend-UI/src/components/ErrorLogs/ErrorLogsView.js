import React, { useState, useEffect } from 'react';
import './ErrorLogs.css';

function ErrorLogsView({ token }) {
    const [apps, setApps] = useState([]);
    const [selectedAppId, setSelectedAppId] = useState('all');
    const [errorLogs, setErrorLogs] = useState([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (!token) return;

        fetch('https://localhost:7057/api/Application', {
            headers: { 'Authorization': `Bearer ${token}` }
        })
            .then(res => res.json())
            .then(data => setApps(data))
            .catch(err => console.error('Error fetching apps:', err));
    }, [token]);

    useEffect(() => {
        if (!token) return;

        setLoading(true);
        const url = selectedAppId === 'all'
            ? 'https://localhost:7057/api/Error/logs'
            : `https://localhost:7057/api/Error/logs/app/${selectedAppId}`;

        fetch(url, {
            headers: { 'Authorization': `Bearer ${token}` }
        })
            .then(res => res.json())
            .then(data => {
                setErrorLogs(data);
                setLoading(false);
            })
            .catch(err => {
                console.error('Error fetching logs:', err);
                setLoading(false);
            });
    }, [token, selectedAppId]);

    return (
        <div className="error-log-wrapper">
            <div className="filter-container">
                <label htmlFor="appFilter">Filter by Application: </label>
                <select
                    id="appFilter"
                    value={selectedAppId}
                    onChange={e => setSelectedAppId(e.target.value)}
                >
                    <option value="all">All Applications</option>
                    {apps.map(app => (
                        <option key={app.id} value={app.id}>
                            {app.name}
                        </option>
                    ))}
                </select>
            </div>

            {loading ? (
                <p className="message">Loading error logs...</p>
            ) : (
                <ul className="error-log-list">
                    {errorLogs.length > 0 ? (
                        errorLogs.map(log => (
                            <li key={log.id} className="error-log-item">
                                <strong>App:</strong> {apps.find(a => a.id === log.appId)?.name || log.appId} -{' '}
                                <strong>Timestamp:</strong> {log.timestamp || 'No timestamp'} -{' '}
                                <strong>Status:</strong> {log.status}
                            </li>
                        ))
                    ) : (
                        <li className="message">No error logs found.</li>
                    )}
                </ul>
            )}
        </div>
    );
}

export default ErrorLogsView;
    