// src\pages\workspaces.tsx
import React from 'react';
import { Box, CircularProgress } from '@mui/material';
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
  return <WorkspacesPageContent />;
};

export default WorkspacesPage;
