// src\pages\workspaces.tsx
import React from 'react';
import { Typography, Box, CircularProgress } from '@mui/material';
import dynamic from 'next/dynamic';

const WorkspacesPageContent = dynamic(() => import('../components/WorkspacesPageContent'), {
  ssr: false,
  loading: () => (
    <Box display="flex" justifyContent="center" alignItems="center" height="50vh">
      <CircularProgress />
    </Box>
  ),
});

const WorkspacesPage: React.FC = () => {
  return (
    <>
      <Typography variant="h4" component="h1" gutterBottom>
        Workspaces
      </Typography>
      <WorkspacesPageContent />
    </>
  );
};

export default WorkspacesPage;
