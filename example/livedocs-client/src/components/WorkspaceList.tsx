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

export interface WorkspaceListProps {
  readonly workspaces: Workspace[];
  readonly onEdit: (workspace: Workspace) => void;
  readonly onDelete: (workspace: Workspace) => void;
}

const WorkspaceList: React.FC<WorkspaceListProps> = ({ workspaces, onEdit, onDelete }) => {
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
                <StyledTableCell>{workspace.name}</StyledTableCell>
                <StyledTableCell>{new Date(workspace.updatedAt).toLocaleString()}</StyledTableCell>
                <StyledTableCell>
                  <IconButton onClick={(): void => onEdit(workspace)} color="primary">
                    <EditIcon />
                  </IconButton>
                  <IconButton onClick={(): void => onDelete(workspace)} color="error">
                    <DeleteIcon />
                  </IconButton>
                </StyledTableCell>
              </motion.tr>
            ))}
          </AnimatePresence>
        </TableBody>
      </Table>
    </TableContainer>
  );
};

export default WorkspaceList;
