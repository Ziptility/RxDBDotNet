import React, { ReactNode, useEffect } from 'react';
import { useRouter } from 'next/router';
import { useAuth } from '@/contexts/AuthContext';
import { CircularProgress, Box } from '@mui/material';

interface ProtectedRouteProps {
  children: ReactNode;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const { isLoggedIn, isInitialized } = useAuth();
  const router = useRouter();

  console.log('ProtectedRoute: Render', { isLoggedIn, isInitialized, pathname: router.pathname });

  useEffect(() => {
    console.log('ProtectedRoute: useEffect', { isLoggedIn, isInitialized, pathname: router.pathname });
    if (isInitialized && !isLoggedIn && router.pathname !== '/login') {
      console.log('ProtectedRoute: Redirecting to login');
      void router.push('/login');
    }
  }, [isLoggedIn, isInitialized, router]);

  if (!isInitialized) {
    console.log('ProtectedRoute: Showing loading spinner (not initialized)');
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="100vh">
        <CircularProgress />
      </Box>
    );
  }

  if (!isLoggedIn && router.pathname !== '/login') {
    console.log('ProtectedRoute: Showing loading spinner (not logged in and not on login page)');
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="100vh">
        <CircularProgress />
      </Box>
    );
  }

  console.log('ProtectedRoute: Rendering children');
  return <>{children}</>;
};

export default ProtectedRoute;
