import React from 'react';
import { Button } from '@mui/material';
import Link from 'next/link';
import { useRouter } from 'next/router';

const navItems = [
  { label: 'Home', path: '/' },
  { label: 'Workspaces', path: '/workspaces' },
  { label: 'Users', path: '/users' },
  { label: 'LiveDocs', path: '/livedocs' },
];

export const NavButtons: React.FC = () => {
  const router = useRouter();
  return (
    <>
      {navItems.map((item) => (
        <Button
          key={item.path}
          color="inherit"
          component={Link}
          href={item.path}
          sx={{
            textTransform: 'none',
            fontWeight: router.pathname === item.path ? 'bold' : 'normal',
            borderBottom: router.pathname === item.path ? '2px solid white' : 'none',
          }}
        >
          {item.label}
        </Button>
      ))}
    </>
  );
};
