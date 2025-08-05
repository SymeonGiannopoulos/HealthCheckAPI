import React, { useEffect, useState } from 'react';
import './AppList.css';

function AppList({ apps, token, onEdit, onDelete, onAddAppClick }) {
    const [healthStatuses, setHealthStatuses] = useState([]);
    const [selectedAppId, setSelectedAppId] = useState(null);
    const [allStats, setAllStats] = useState({});
    const [allErrorLogs, setAllErrorLogs] = useState({});
    const [loading, setLoading] = useState(false);

    // Health check
    useEffect(() => {
        if (!token) return;

        fetch('https://localhost:7057/Health/check-all-health', {
            headers: { 'Authorization': `Bearer ${token}` }
        })
            .then(res => res.json())
            .then(data => setHealthStatuses(data))
            .catch(err => console.error(err));
    }, [token]);

    // Fetch all stats once
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
    }, [token, apps]);

    const getBorderColor = (appId) => {
        const status = healthStatuses.find(s => s.id === appId);
        if (!status) return '2px solid gray';
        return status.status === 'Healthy' ? '3px solid #4caf50' : '3px solid #f44336';
    };

    const getBackgroundColor = (appId) => {
        const status = healthStatuses.find(s => s.id === appId);
        if (!status) return 'white';
        return status.status === 'Healthy' ? '#e8f5e9' : '#ffebee';
    };

    const onAppClick = (appId) => {
        if (selectedAppId === appId) {
            setSelectedAppId(null);
            setAllErrorLogs({});
            return;
        }

        setSelectedAppId(appId);
        setLoading(true);

        fetch(`https://localhost:7057/api/Error/logs/app/${appId}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        })
            .then(res => res.json())
            .then(data => {
                setAllErrorLogs(prev => ({ ...prev, [appId]: data }));
                setLoading(false);
            })
            .catch(err => {
                console.error(err);
                setLoading(false);
            });
    };

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
                            border: getBorderColor(app.id),
                            backgroundColor: getBackgroundColor(app.id),
                            position: 'relative',
                        }}
                        onClick={() => onAppClick(app.id)}
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
                                            errorLogs.map(log => (
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

                        {/* Edit Button */}
                        <button
                            onClick={e => { e.stopPropagation(); onEdit(app); }}
                            aria-label="Edit"
                            title="Edit"
                        >
                            ✏️
                        </button>

                        {/* Delete Button */}
                        <button
                            onClick={e => { e.stopPropagation(); onDelete(app.id); }}
                            aria-label="Delete"
                            title="Delete"
                        >
                            ×
                        </button>
                    </div>
                );
            })}

            {/* Add Application Card */}
            {onAddAppClick && (
                <div
                    className="app-card add-card"
                    onClick={onAddAppClick}
                >
                    <span className="add-text">+ Add Application</span>
                </div>
            )}
        </div>
    );


}

export default AppList;
