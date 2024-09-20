import React from 'react';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import type { Workspace } from '@/lib/schemas';
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
          {workspaces.map((workspace) => (
            <StyledTableRow key={workspace.id}>
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
            </StyledTableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};

export default WorkspaceList;
