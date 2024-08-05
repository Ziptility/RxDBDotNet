import React, { useState, useEffect } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
} from '@mui/material';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { LiveDocsDatabase } from '@/types';
import { WorkspaceDocType } from '@/lib/schemas';

interface WorkspaceListProps {
  db: LiveDocsDatabase;
  onEdit: (workspace: WorkspaceDocType) => void | Promise<void>;
  onDelete: (workspace: WorkspaceDocType) => void | Promise<void>;
}

const WorkspaceList: React.FC<WorkspaceListProps> = ({ db, onEdit, onDelete }) => {
  const [workspaces, setWorkspaces] = useState<WorkspaceDocType[]>([]);

  useEffect(() => {
    const subscription = db.workspaces
      .find({
        selector: {
          isDeleted: false,
        },
        sort: [{ updatedAt: 'desc' }],
      })
      .$.subscribe((docs) => {
        setWorkspaces(docs.map((doc) => doc.toJSON()));
      });

    return () => subscription.unsubscribe();
  }, [db]);

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
                <IconButton onClick={() => void onEdit(workspace)}>
                  <EditIcon />
                </IconButton>
                <IconButton onClick={() => void onDelete(workspace)}>
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
