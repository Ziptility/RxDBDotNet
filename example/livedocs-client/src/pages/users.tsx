// src\pages\users.tsx
import React from 'react';
import { Typography, Box, CircularProgress } from '@mui/material';
import dynamic from 'next/dynamic';

const UsersPageContent = dynamic(() => import('../components/UsersPageContent'), {
  ssr: false,
  loading: () => (
    <Box display="flex" justifyContent="center" alignItems="center" height="50vh">
      <CircularProgress />
    </Box>
  ),
});

const UsersPage: React.FC = () => {
  return (
    <>
      <Typography variant="h4" component="h1" gutterBottom>
        Users
      </Typography>
      <UsersPageContent />
    </>
  );
};

export default UsersPage;
