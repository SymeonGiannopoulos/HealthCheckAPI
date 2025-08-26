import React, { useEffect, useState } from "react";
import axiosInstance from "../../axiosInstance";
import "./UserListView.css";

const UserListView = () => {
    const [users, setUsers] = useState([]);
    const [userToDelete, setUserToDelete] = useState(null);
    const [showModal, setShowModal] = useState(false);

    useEffect(() => {
        fetchUsers();
    }, []);

    const fetchUsers = async () => {
        try {
            const response = await axiosInstance.get("/User");
            setUsers(response.data);
        } catch (error) {
            console.error("Σφάλμα κατά τη λήψη χρηστών");
        }
    };
    const handleDeleteClick = (user) => {
        setUserToDelete(user);
        setShowModal(true);
    };

    const handleCancelDelete = () => {
        setUserToDelete(null);
        setShowModal(false);
    };

    const handleConfirmDelete = async () => {
        if (!userToDelete) return;

        try {
            await axiosInstance.delete(`/User/${userToDelete.id}`);
            setUsers(users.filter(user => user.id !== userToDelete.id));
        } catch (error) {
            console.error("Αποτυχία διαγραφής χρήστη");
        } finally {
            setUserToDelete(null);
            setShowModal(false);
        }
    };

    return (
        <div className="user-list-container">
            <div className="user-list">
                {users.map(user => (
                    <div key={user.id} className="user-card">
                        <div className="user-info">
                            <p><strong>ID:</strong> {user.id}</p>
                            <p><strong>Username:</strong> {user.username}</p>
                            <p><strong>Email:</strong> {user.email}</p>
                        </div>
                        <button
                            className="user-delete-button"
                            onClick={() => handleDeleteClick(user)}
                            title="Διαγραφή Χρήστη"
                        >
                            ×
                        </button>
                    </div>
                ))}
            </div>

            {showModal && (
                <div className="modal-overlay">
                    <div className="modal">
                        <p>Θέλεις σίγουρα να διαγράψεις τον χρήστη <strong>{userToDelete?.username}</strong>;</p>
                        <div className="modal-actions">
                            <button className="modal-button-cancel" onClick={handleCancelDelete}>Ακύρωση</button>
                            <button className="modal-button-confirm" onClick={handleConfirmDelete}>Διαγραφή</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default UserListView;
