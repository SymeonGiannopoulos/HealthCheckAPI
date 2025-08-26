import React, { useEffect, useState } from 'react';
import './AppList.css';

function AppList({ apps, token, onEdit, onDelete, onAddAppClick, refreshKey }) {
    const [healthStatuses, setHealthStatuses] = useState([]);
    const [selectedAppId, setSelectedAppId] = useState(null);
    const [allStats, setAllStats] = useState({});
    const [allErrorLogs, setAllErrorLogs] = useState({});
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (!token) return;

        fetch('https://localhost:7057/Health/check-all-health', {
            headers: { 'Authorization': `Bearer ${token}` }
        })
            .then(res => res.json())
            .then(data => setHealthStatuses(data))
            .catch(err => console.error(err));
    }, [token, refreshKey]); 

    useEffect(() => {
        if (!token || apps.length === 0) return;

        setLoading(true);

        Promise.all(
            apps.map(app =>
                fetch(`https://localhost:7057/api/AppStatistics/${app.id}`, {
                    headers: { 'Authorization': `Bearer ${token}` }
                })
                    .then(res => res.json())
                    .then(data => ({ appId: app.id, data }))
                    .catch(err => {
                        console.error(`Error fetching stats for app ${app.id}:`, err);
                        return { appId: app.id, data: null };
                    })
            )
        ).then(results => {
            const statsMap = {};
            results.forEach(({ appId, data }) => {
                statsMap[appId] = data;
            });
            setAllStats(statsMap);
            setLoading(false);
        });
    }, [token, apps, refreshKey]); 

 

    return (
        <div className="app-list">
            {apps.map(app => {
                const isSelected = app.id === selectedAppId;
                const stats = allStats[app.id];
                const errorLogs = allErrorLogs[app.id] || [];

                return (
                    <div
                        key={app.id}
                        className={`app-card ${isSelected ? 'selected' : ''}`}
                        style={{
                            border: (() => {
                                const status = healthStatuses.find(s => s.id === app.id);
                                if (!status) return '2px solid gray';
                                return status.status === 'Healthy' ? '3px solid #4caf50' : '3px solid #f44336';
                            })(),
                            backgroundColor: (() => {
                                const status = healthStatuses.find(s => s.id === app.id);
                                if (!status) return 'white';
                                return status.status === 'Healthy' ? '#e8f5e9' : '#ffebee';
                            })(),
                            position: 'relative',
                        }}
                        onClick={() => {
                            if (selectedAppId === app.id) {
                                setSelectedAppId(null);
                                setAllErrorLogs({});
                                return;
                            }

                            setSelectedAppId(app.id);
                            setLoading(true);

                            fetch(`https://localhost:7057/api/Error/logs/app/${app.id}`, {
                                headers: { 'Authorization': `Bearer ${token}` }
                            })
                                .then(res => res.json())
                                .then(data => {
                                    setAllErrorLogs(prev => ({ ...prev, [app.id]: data }));
                                    setLoading(false);
                                })
                                .catch(err => {
                                    console.error(err);
                                    setLoading(false);
                                });
                        }}
                    >
                        <div className="info-and-stats">
                            <div className="appinfo">
                                <span><strong>ID:</strong> {app.id}</span>
                                <span><strong>Name:</strong> {app.name}</span>
                                <span><strong>Type:</strong> {app.type}</span>
                            </div>

                            {stats && (
                                <div className="stats">
                                    <span><strong>Avg Downtime:</strong> {stats.averageDowntimeMinutes} min</span>
                                    <span><strong>Downtimes:</strong> {stats.downtimesCount}</span>
                                    <span><strong>Total Downtime:</strong> {stats.totalDowntime} min</span>
                                    <span><strong>Availability:</strong> {stats.availabilityPercent}%</span>
                                </div>
                            )}
                        </div>

                        {isSelected && (
                            <div className="details">
                                <strong>Error Logs</strong>
                                {loading ? (
                                    <p>Loading...</p>
                                ) : (
                                    <ul>
                                        {errorLogs.length > 0 ? (
                                            errorLogs.slice(-3).map(log => (
                                                <li key={log.id}>
                                                    {log.timestamp} - Status: {log.status}
                                                </li>
                                            ))
                                        ) : (
                                            <li>No recent errors</li>
                                        )}
                                    </ul>
                                )}
                            </div>
                        )}

                        <button
                            aria-label="Edit"
                            onClick={e => { e.stopPropagation(); onEdit(app); }}
                        >✏️</button>
                        <button
                            aria-label="Delete"
                            onClick={e => { e.stopPropagation(); onDelete(app.id); }}
                        >×</button>

                    </div>
                );
            })}

            {/* Add Application Card */}
            {onAddAppClick && (
                <div className="app-card add-card" onClick={onAddAppClick}>
                    <span className="add-text">+ Add Application</span>
                </div>
            )}
        </div>
    );
}

export default AppList;
