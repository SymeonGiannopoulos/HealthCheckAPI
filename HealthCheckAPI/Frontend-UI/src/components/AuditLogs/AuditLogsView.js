import React, { useEffect, useState } from "react";
import axiosInstance from "../../axiosInstance";
import './AuditLogs.css';

const AuditLogsView = () => {
    const [logs, setLogs] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [limit, setLimit] = useState(100);
    const [selectedLogs, setSelectedLogs] = useState(new Set());


    useEffect(() => {
        const fetchLogs = async () => {
            setLoading(true);
            try {
                const response = await axiosInstance.get("/api/AuditLog", {
                    params: { limit }
                });
                setLogs(response.data);
            } catch (err) {
                setError("Αποτυχία λήψης δεδομένων.");
            } finally {
                setLoading(false);
            }
        };

        fetchLogs();
    }, [limit]);

    const handleDeleteSelected = async () => {
        if (selectedLogs.size === 0) return;

        if (!window.confirm(`Θέλεις να διαγράψεις ${selectedLogs.size} επιλεγμένα logs;`)) {
            return;
        }

        try {
            const idsToDelete = Array.from(selectedLogs);

            await axiosInstance.post("/api/AuditLog/delete-many", idsToDelete);

            setLogs((prevLogs) => prevLogs.filter(log => !selectedLogs.has(log.id)));

            setSelectedLogs(new Set());
        } catch (error) {
            alert("Σφάλμα κατά τη διαγραφή των logs.");
        }
    };


    return (
        <div className="audit-logs-container">
            {selectedLogs.size > 0 && (
                <div style={{ marginBottom: '20px' }}>
                    <button className="delete-selected-btn" onClick={handleDeleteSelected}>
                        Διαγραφή Επιλεγμένων ({selectedLogs.size})
                    </button>
                </div>
            )}
            <div className="audit-logs-controls">
                <label htmlFor="limit">Registrations:</label>
                <input
                    id="limit"
                    type="number"
                    value={limit}
                    onChange={(e) => setLimit(Number(e.target.value))}
                    min="1"
                    placeholder="Limit"
                    className="audit-logs-limit-input"
                />
            </div>

            {loading ? (
                <p>Φόρτωση...</p>
            ) : error ? (
                <p className="audit-logs-message audit-logs-error">{error}</p>
            ) : logs.length === 0 ? (
                <p className="audit-logs-message">Δεν υπάρχουν καταγεγραμμένα logs.</p>
            ) : (
                <div className="audit-logs-table-wrapper">
                    <table className="audit-logs-table">
                                    <thead>
                                        <tr>
                                            <th>
                                                <input
                                                    type="checkbox"
                                                    onChange={(e) => {
                                                        if (e.target.checked) {
                                                            setSelectedLogs(new Set(logs.map((log) => log.id)));
                                                        } else {
                                                            setSelectedLogs(new Set());
                                                        }
                                                    }}
                                                    checked={logs.length > 0 && selectedLogs.size === logs.length}
                                                />
                                            </th>
                                            <th>ID</th>
                                            <th>User</th>
                                            <th>Date</th>
                                            <th>Action</th>
                                            <th>Entity</th>
                                            <th>Entity ID</th>
                                            <th>Details</th>
                                            <th>IP</th>
                                        </tr>
                                    </thead>

                                    <tbody>
                                        {logs.map((log) => (
                                            <tr
                                                key={log.id}
                                                className={
                                                    log.details?.toLowerCase().includes("error")
                                                        ? "audit-logs-error-row"
                                                        : ""
                                                }
                                            >
                                                <td>
                                                    <input
                                                        type="checkbox"
                                                        checked={selectedLogs.has(log.id)}
                                                        onChange={(e) => {
                                                            const newSelected = new Set(selectedLogs);
                                                            if (e.target.checked) {
                                                                newSelected.add(log.id);
                                                            } else {
                                                                newSelected.delete(log.id);
                                                            }
                                                            setSelectedLogs(newSelected);
                                                        }}
                                                    />
                                                </td>
                                                <td>{log.id}</td>
                                                <td>{log.userId}</td>
                                                <td>{new Date(log.timestamp).toLocaleString()}</td>
                                                <td>{log.actionType}</td>
                                                <td>{log.entityType}</td>
                                                <td>{log.entityId ?? "-"}</td>
                                                <td>{log.details}</td>
                                                <td>{log.ipAddress}</td>
                                            </tr>
                                        ))}
                                    </tbody>

                    </table>
                </div>
            )}
        </div>
    );
};

export default AuditLogsView;
