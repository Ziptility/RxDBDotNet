// example/livedocs-client/src/components/NavigationRail.tsx
import React, { useState, useCallback } from 'react';
import {
  Home as HomeIcon,
  Group as GroupIcon,
  Work as WorkIcon,
  Description as DescriptionIcon,
  AccountCircle as AccountCircleIcon,
  Logout as LogoutIcon,
} from '@mui/icons-material';
import {
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Paper,
  Avatar,
  Menu,
  MenuItem,
  Divider,
  Box,
  Typography,
} from '@mui/material';
import Link from 'next/link';
import { useRouter } from 'next/router';
import { useAuth } from '@/contexts/AuthContext';
import NetworkStatus from './NetworkStatus';

interface NavItem {
  readonly label: string;
  readonly path: string;
  readonly icon: React.ReactElement;
  readonly ariaLabel: string;
}

const navItems: NavItem[] = [
  { label: 'Home', path: '/', icon: <HomeIcon />, ariaLabel: 'Go to Home' },
  { label: 'Workspaces', path: '/workspaces', icon: <WorkIcon />, ariaLabel: 'Go to Workspaces' },
  { label: 'Users', path: '/users', icon: <GroupIcon />, ariaLabel: 'Go to Users' },
  { label: 'LiveDocs', path: '/livedocs', icon: <DescriptionIcon />, ariaLabel: 'Go to LiveDocs' },
];

const NavigationRail: React.FC = () => {
  const router = useRouter();
  const { currentUser, logout } = useAuth();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [isExpanded, setIsExpanded] = useState(false);

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>): void => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = (): void => {
    setAnchorEl(null);
  };

  const handleLogout = useCallback(async (): Promise<void> => {
    await logout();
    handleMenuClose();
    await router.push('/login');
  }, [logout, router]);

  return (
    <Paper
      elevation={3}
      component="nav"
      aria-label="Main Navigation"
      onMouseEnter={(): void => setIsExpanded(true)}
      onMouseLeave={(): void => setIsExpanded(false)}
      sx={(theme) => ({
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
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
      })}
    >
      <List component="div">
        {navItems.map((item) => {
          const isActive = router.pathname === item.path;
          return (
            <Link key={item.path} href={item.path} passHref legacyBehavior>
              <ListItemButton
                component="a"
                aria-label={item.ariaLabel}
                aria-current={isActive ? 'page' : undefined}
                sx={(theme) => ({
                  minHeight: 48,
                  justifyContent: 'initial',
                  px: 2.5,
                  color: isActive ? theme.palette.primary.main : theme.palette.text.primary,
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
                })}
              >
                <ListItemIcon>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} />
              </ListItemButton>
            </Link>
          );
        })}
      </List>
      <Box>
        <Box sx={{ p: 2, display: 'flex', justifyContent: 'center', alignItems: 'center', width: '100%' }}>
          <NetworkStatus expanded={isExpanded} />
        </Box>
        <Box sx={{ p: 2, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}>
          <Avatar sx={{ width: 40, height: 40, mb: 1, cursor: 'pointer' }} onClick={handleMenuOpen}>
            <AccountCircleIcon />
          </Avatar>
          <Typography variant="caption" noWrap sx={{ maxWidth: '100%', textAlign: 'center' }}>
            {currentUser?.firstName}
          </Typography>
        </Box>
      </Box>
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
        anchorOrigin={{
          vertical: 'top',
          horizontal: 'right',
        }}
        transformOrigin={{
          vertical: 'bottom',
          horizontal: 'left',
        }}
      >
        <MenuItem>
          <Typography variant="subtitle1">
            {currentUser?.firstName} {currentUser?.lastName}
          </Typography>
        </MenuItem>
        <MenuItem>
          <Typography variant="body2" color="textSecondary">
            {currentUser?.email}
          </Typography>
        </MenuItem>
        <Divider />
        <MenuItem
          onClick={(): void => {
            void handleLogout();
          }}
        >
          <ListItemIcon>
            <LogoutIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText primary="Logout" />
        </MenuItem>
      </Menu>
    </Paper>
  );
};

export default NavigationRail;
