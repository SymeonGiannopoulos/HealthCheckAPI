import React, { useEffect, useState } from "react";
import axiosInstance from "../../axiosInstance";

const AuditLogsView = () => {
    const [logs, setLogs] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [limit, setLimit] = useState(100);

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




    return (
        <div className="audit-log-container">
            <div style={{ display: "flex", alignItems: "center", gap: "12px", marginBottom: "16px" }}>
                <label htmlFor="limit">Registrations:</label>
                <input
                    id="limit"
                    type="number"
                    value={limit}
                    onChange={(e) => setLimit(Number(e.target.value))}
                    min="1"
                    placeholder="Limit"
                    className="limit-input"
                />
            </div> 
            {loading ? (
                <p>Φόρτωση...</p>
            ) : error ? (
                <p className="message error">{error}</p>
            ) : logs.length === 0 ? (
                <p className="message">Δεν υπάρχουν καταγεγραμμένα logs.</p>
            ) : (
                <div style={{ width: "100%", display: "flex", justifyContent: "center" }}>
                    <table style={{ width: "100%", borderCollapse: "collapse" }}>
                        <thead>
                            <tr style={{ backgroundColor: "#e0e0e0" }}>
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
                                <tr key={log.id}>
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
