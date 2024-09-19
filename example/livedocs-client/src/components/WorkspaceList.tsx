// src\components\WorkspaceList.tsx
import React from 'react';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, IconButton } from '@mui/material';
import type { Workspace } from '@/lib/schemas';

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
            <TableCell>Name</TableCell>
            <TableCell>Updated At</TableCell>
            <TableCell>Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {workspaces.map((workspace) => (
            <TableRow key={workspace.id}>
              <TableCell>{workspace.name}</TableCell>
              <TableCell>{new Date(workspace.updatedAt).toLocaleString()}</TableCell>
              <TableCell>
                <IconButton onClick={(): void => onEdit(workspace)}>
                  <EditIcon />
                </IconButton>
                <IconButton onClick={(): void => onDelete(workspace)}>
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

export default WorkspaceList;
