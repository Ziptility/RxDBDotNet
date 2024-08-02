import React from 'react';
import { Typography, Box } from '@mui/material';

const Home: React.FC = () => {
  return (
    <Box sx={{ textAlign: 'center' }}>
      <Typography variant="h2" component="h1" gutterBottom>
        Welcome to LiveDocs
      </Typography>
      <Typography variant="h5" component="h2" gutterBottom>
        A real-time collaborative document management system
      </Typography>
      <Typography variant="body1">
        Use the navigation links above to manage workspaces, users, and live documents.
      </Typography>
    </Box>
  );
};

export default Home;