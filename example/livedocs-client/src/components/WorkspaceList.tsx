import React from 'react';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { motion, AnimatePresence } from 'framer-motion';
import type { Workspace } from '@/lib/schemas';
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
import WorkspaceForm from './WorkspaceForm';

export interface WorkspaceListProps {
  readonly workspaces: Workspace[];
  readonly editingWorkspaceId: string | null;
  readonly onEdit: (workspaceId: string) => void;
  readonly onCancelEdit: () => void;
  readonly onDelete: (workspaceId: string) => void;
  readonly onSubmit: (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>) => void;
}

const WorkspaceList: React.FC<WorkspaceListProps> = ({
  workspaces,
  editingWorkspaceId,
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
            <StyledTableCell>Updated At</StyledTableCell>
            <StyledTableCell>Actions</StyledTableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          <AnimatePresence>
            {workspaces.map((workspace) => (
              <motion.tr
                key={workspace.id}
                {...motionProps['fadeIn']}
                layout
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
              >
                {editingWorkspaceId === workspace.id ? (
                  <StyledTableCell colSpan={3}>
                    <WorkspaceForm workspace={workspace} onSubmit={onSubmit} onCancel={onCancelEdit} isInline />
                  </StyledTableCell>
                ) : (
                  <>
                    <StyledTableCell>{workspace.name}</StyledTableCell>
                    <StyledTableCell>{new Date(workspace.updatedAt).toLocaleString()}</StyledTableCell>
                    <StyledTableCell>
                      <IconButton onClick={() => onEdit(workspace.id)} color="primary">
                        <EditIcon />
                      </IconButton>
                      <IconButton onClick={() => onDelete(workspace.id)} color="error">
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

export default WorkspaceList;
