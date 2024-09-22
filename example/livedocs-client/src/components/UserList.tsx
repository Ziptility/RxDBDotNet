import React from 'react';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { motion, AnimatePresence } from 'framer-motion';
import type { User, Workspace } from '@/lib/schemas';
import {
  TableContainer,
  Table,
  TableHead,
  TableRow,
  TableBody,
  StyledTableCell,
  Paper,
  IconButton,
} from '@/styles/StyledComponents';
import { motionProps } from '@/utils/motionSystem';
import UserForm from './UserForm';

export interface UserListProps {
  readonly users: User[];
  readonly workspaces: Workspace[];
  readonly editingUserId: string | null;
  readonly onEdit: (userId: string) => void;
  readonly onCancelEdit: () => void;
  readonly onDelete: (userId: string) => void;
  readonly onSubmit: (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>) => void;
}

const UserList: React.FC<UserListProps> = ({
  users,
  workspaces,
  editingUserId,
  onEdit,
  onCancelEdit,
  onDelete,
  onSubmit,
}) => {
  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <StyledTableCell>Name</StyledTableCell>
            <StyledTableCell>Email</StyledTableCell>
            <StyledTableCell>Role</StyledTableCell>
            <StyledTableCell>Workspace</StyledTableCell>
            <StyledTableCell>Actions</StyledTableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          <AnimatePresence>
            {users.map((user) => (
              <motion.tr
                key={user.id}
                {...motionProps['fadeIn']}
                layout
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
              >
                {editingUserId === user.id ? (
                  <StyledTableCell colSpan={5}>
                    <UserForm
                      user={user}
                      workspaces={workspaces}
                      onSubmit={(data) => {
                        void onSubmit(data);
                      }}
                      onCancel={onCancelEdit}
                      isInline
                    />
                  </StyledTableCell>
                ) : (
                  <>
                    <StyledTableCell>{`${user.firstName} ${user.lastName}`}</StyledTableCell>
                    <StyledTableCell>{user.email}</StyledTableCell>
                    <StyledTableCell>{user.role}</StyledTableCell>
                    <StyledTableCell>
                      {workspaces.find((w) => w.id === user.workspaceId)?.name ?? 'Unknown'}
                    </StyledTableCell>
                    <StyledTableCell>
                      <IconButton onClick={() => onEdit(user.id)} color="primary">
                        <EditIcon />
                      </IconButton>
                      <IconButton onClick={() => onDelete(user.id)} color="error">
                        <DeleteIcon />
                      </IconButton>
                    </StyledTableCell>
                  </>
                )}
              </motion.tr>
            ))}
          </AnimatePresence>
        </TableBody>
      </Table>
    </TableContainer>
  );
};

export default UserList;
