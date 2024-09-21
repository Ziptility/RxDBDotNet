// src/components/NavigationRail.tsx
import React from 'react';
import {
  Home as HomeIcon,
  Group as GroupIcon,
  Work as WorkIcon,
  Description as DescriptionIcon,
} from '@mui/icons-material';
import { List, ListItem, ListItemButton, ListItemIcon, ListItemText, Paper, styled } from '@mui/material';
import Link from 'next/link';
import { useRouter } from 'next/router';

const StyledPaper = styled(Paper)(({ theme }) => ({
  height: '100vh',
  width: '80px',
  position: 'fixed',
  top: 0,
  left: 0,
  zIndex: theme.zIndex.appBar,
  transition: theme.transitions.create('width', {
    easing: theme.transitions.easing.sharp,
    duration: theme.transitions.duration.leavingScreen,
  }),
  '&:hover': {
    width: '240px',
  },
}));

const StyledListItem = styled(ListItem)<{ active?: boolean }>(({ theme, active }) => ({
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  padding: theme.spacing(2, 1),
  color: (active ?? false) ? theme.palette.primary.main : theme.palette.text.primary,
  '& .MuiListItemIcon-root': {
    minWidth: 'auto',
    marginBottom: theme.spacing(0.5),
  },
  '& .MuiListItemText-root': {
    display: 'none',
  },
  '&:hover .MuiListItemText-root': {
    display: 'block',
  },
}));

const navItems = [
  { label: 'Home', path: '/', icon: <HomeIcon /> },
  { label: 'Workspaces', path: '/workspaces', icon: <WorkIcon /> },
  { label: 'Users', path: '/users', icon: <GroupIcon /> },
  { label: 'LiveDocs', path: '/livedocs', icon: <DescriptionIcon /> },
];

const NavigationRail: React.FC = () => {
  const router = useRouter();

  return (
    <StyledPaper elevation={3}>
      <List>
        {navItems.map((item) => (
          <StyledListItem key={item.path} active={router.pathname === item.path}>
            <ListItemButton component={Link} href={item.path}>
              <ListItemIcon>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} />
            </ListItemButton>
          </StyledListItem>
        ))}
      </List>
    </StyledPaper>
  );
};

export default NavigationRail;
