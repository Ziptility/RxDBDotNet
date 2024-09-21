// src\components\UserList.tsx
import React from 'react';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import type { User } from '@/lib/schemas';
import {
  TableContainer,
  Table,
  TableHead,
  TableRow,
  TableBody,
  StyledTableCell,
  StyledTableRow,
  Paper,
  IconButton,
} from '@/styles/StyledComponents';

export interface UserListProps {
  readonly users: User[];
  readonly onEdit: (user: User) => void;
  readonly onDelete: (user: User) => void;
}

const UserList: React.FC<UserListProps> = ({ users, onEdit, onDelete }) => {
  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <StyledTableCell>Name</StyledTableCell>
            <StyledTableCell>Email</StyledTableCell>
            <StyledTableCell>Role</StyledTableCell>
            <StyledTableCell>Workspace ID</StyledTableCell>
            <StyledTableCell>Updated At</StyledTableCell>
            <StyledTableCell>Actions</StyledTableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {users.map((user) => (
            <StyledTableRow key={user.id}>
              <StyledTableCell>{`${user.firstName} ${user.lastName}`}</StyledTableCell>
              <StyledTableCell>{user.email}</StyledTableCell>
              <StyledTableCell>{user.role}</StyledTableCell>
              <StyledTableCell>{user.workspaceId}</StyledTableCell>
              <StyledTableCell>{new Date(user.updatedAt).toLocaleString()}</StyledTableCell>
              <StyledTableCell>
                <IconButton onClick={(): void => onEdit(user)} color="primary">
                  <EditIcon />
                </IconButton>
                <IconButton onClick={(): void => onDelete(user)} color="error">
                  <DeleteIcon />
                </IconButton>
              </StyledTableCell>
            </StyledTableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};

export default UserList;
