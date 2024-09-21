// src/components/NavigationRail.tsx
import React from 'react';
import {
  Home as HomeIcon,
  Group as GroupIcon,
  Work as WorkIcon,
  Description as DescriptionIcon,
} from '@mui/icons-material';
import { List, ListItemButton, ListItemIcon, ListItemText, Paper, styled } from '@mui/material';
import Link from 'next/link';
import { useRouter } from 'next/router';

const StyledPaper = styled(Paper)(({ theme }) => ({
  position: 'fixed',
  top: 0,
  left: 0,
  height: '100vh',
  width: '80px',
  transition: theme.transitions.create('width', {
    easing: theme.transitions.easing.sharp,
    duration: theme.transitions.duration.leavingScreen,
  }),
  overflowX: 'hidden',
  '&:hover': {
    width: '240px',
    overflowX: 'visible',
  },
  zIndex: theme.zIndex.appBar,
}));

const StyledListItemButton = styled(ListItemButton)<{ active: boolean }>(({ theme, active }) => ({
  minHeight: 48,
  justifyContent: 'initial',
  px: 2.5,
  color: active ? theme.palette.primary.main : theme.palette.text.primary,
  '& .MuiListItemIcon-root': {
    minWidth: 0,
    marginRight: theme.spacing(3),
    justifyContent: 'center',
    color: 'inherit',
  },
  '& .MuiListItemText-primary': {
    opacity: 0,
    transition: theme.transitions.create('opacity', {
      duration: theme.transitions.duration.shorter,
    }),
  },
  '&:hover .MuiListItemText-primary': {
    opacity: 1,
  },
}));

const navItems = [
  { label: 'Home', path: '/', icon: <HomeIcon />, ariaLabel: 'Go to Home' },
  { label: 'Workspaces', path: '/workspaces', icon: <WorkIcon />, ariaLabel: 'Go to Workspaces' },
  { label: 'Users', path: '/users', icon: <GroupIcon />, ariaLabel: 'Go to Users' },
  { label: 'LiveDocs', path: '/livedocs', icon: <DescriptionIcon />, ariaLabel: 'Go to LiveDocs' },
];

const NavigationRail: React.FC = () => {
  const router = useRouter();

  return (
    <StyledPaper elevation={3} role="navigation" aria-label="Main Navigation">
      <List>
        {navItems.map((item) => {
          const isActive = router.pathname === item.path;
          return (
            <Link key={item.path} href={item.path} passHref legacyBehavior>
              <StyledListItemButton
                active={isActive}
                LinkComponent="a"
                aria-label={item.ariaLabel}
                aria-current={isActive ? 'page' : undefined}
              >
                <ListItemIcon>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} />
              </StyledListItemButton>
            </Link>
          );
        })}
      </List>
    </StyledPaper>
  );
};

export default NavigationRail;
