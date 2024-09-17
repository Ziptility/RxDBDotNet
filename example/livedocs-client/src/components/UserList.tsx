// src\components\UserList.tsx
import React, { useState, useEffect } from 'react';
import { Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, IconButton } from '@mui/material';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { LiveDocsDatabase } from '@/types';
import { User } from '@/lib/schemas';

interface UserListProps {
  db: LiveDocsDatabase;
  onEdit: (user: User) => void;
  onDelete: (user: User) => void;
}

const UserList: React.FC<UserListProps> = ({ db, onEdit, onDelete }): JSX.Element => {
  const [users, setUsers] = useState<User[]>([]);

  useEffect(() => {
    const subscription = db.users
      .find({
        selector: {
          isDeleted: false,
        },
        sort: [{ updatedAt: 'desc' }],
      })
      .$.subscribe((docs) => {
        setUsers(docs.map((doc) => doc.toJSON()));
      });

    return (): void => subscription.unsubscribe();
  }, [db]);

  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Email</TableCell>
            <TableCell>Role</TableCell>
            <TableCell>Workspace ID</TableCell>
            <TableCell>Updated At</TableCell>
            <TableCell>Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {users.map((user) => (
            <TableRow key={user.id}>
              <TableCell>{`${user.firstName} ${user.lastName}`}</TableCell>
              <TableCell>{user.email}</TableCell>
              {/* <TableCell>{user.role}</TableCell> */}
              <TableCell>{user.workspaceId}</TableCell>
              <TableCell>{new Date(user.updatedAt).toLocaleString()}</TableCell>
              <TableCell>
                <IconButton onClick={(): void => onEdit(user)}>
                  <EditIcon />
                </IconButton>
                <IconButton onClick={(): void => onDelete(user)}>
                  <DeleteIcon />
                </IconButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};

export default UserList;
