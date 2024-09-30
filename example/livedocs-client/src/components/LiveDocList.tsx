// src\components\LiveDocList.tsx
import React from 'react';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { motion, AnimatePresence } from 'framer-motion';
import type { LiveDoc } from '@/generated/graphql';
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

export interface LiveDocListProps {
  readonly liveDocs: LiveDoc[];
  readonly onEdit: (liveDoc: LiveDoc) => void;
  readonly onDelete: (liveDoc: LiveDoc) => void;
}

const LiveDocList: React.FC<LiveDocListProps> = ({ liveDocs, onEdit, onDelete }) => {
  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <StyledTableCell>Content</StyledTableCell>
            <StyledTableCell>Owner ID</StyledTableCell>
            <StyledTableCell>Workspace ID</StyledTableCell>
            <StyledTableCell>Updated At</StyledTableCell>
            <StyledTableCell>Actions</StyledTableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          <AnimatePresence>
            {liveDocs.map((liveDoc) => (
              <motion.tr
                key={liveDoc.id}
                {...motionProps['fadeIn']}
                layout
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
              >
                <StyledTableCell>{liveDoc.content.substring(0, 50)}...</StyledTableCell>
                <StyledTableCell>{liveDoc.ownerId}</StyledTableCell>
                <StyledTableCell>{liveDoc.workspaceId}</StyledTableCell>
                <StyledTableCell>{new Date(liveDoc.updatedAt).toLocaleString()}</StyledTableCell>
                <StyledTableCell>
                  <IconButton onClick={(): void => onEdit(liveDoc)} color="primary">
                    <EditIcon />
                  </IconButton>
                  <IconButton onClick={(): void => onDelete(liveDoc)} color="error">
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

export default LiveDocList;
