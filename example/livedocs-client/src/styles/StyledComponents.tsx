import {
  Box,
  Button,
  CircularProgress,
  Container,
  Paper,
  FormControl,
  InputLabel,
  Typography,
  Alert,
  Select,
  TextField,
  TableCell,
  TableRow,
  Fab,
  AppBar,
  Toolbar,
} from '@mui/material';
import { styled, alpha } from '@mui/material/styles';

// Enhanced PageContainer with responsive padding
export const PageContainer = styled(Container)(({ theme }) => ({
  paddingTop: theme.spacing(4),
  paddingBottom: theme.spacing(4),
  [theme.breakpoints.up('sm')]: {
    paddingTop: theme.spacing(6),
    paddingBottom: theme.spacing(6),
  },
}));

// Updated ContentPaper with Material 3 elevation
export const ContentPaper = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(3),
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius * 2, // More pronounced rounding
  boxShadow: theme.shadows[1], // Subtle elevation
  transition: theme.transitions.create(['box-shadow']),
  '&:hover': {
    boxShadow: theme.shadows[4], // Increased elevation on hover
  },
}));

// Enhanced typography components
export const PageTitle = styled(Typography)(({ theme }) => ({
  marginBottom: theme.spacing(3),
  color: theme.palette.primary.main,
  fontWeight: 700, // Bolder weight for emphasis
}));

export const SectionTitle = styled(Typography)(({ theme }) => ({
  marginBottom: theme.spacing(2),
  color: theme.palette.text.primary,
  fontWeight: 600,
}));

// Form components with Material 3 styling
export const FormContainer = styled(Box)(({ theme }) => ({
  display: 'flex',
  flexDirection: 'column',
  gap: theme.spacing(3), // Increased spacing between form elements
}));

// Enhanced button components
export const PrimaryButton = styled(Button)(({ theme }) => ({
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  borderRadius: theme.shape.borderRadius * 4, // Pill-shaped button
  padding: theme.spacing(1.5, 3),
  textTransform: 'none',
  fontWeight: 500,
  '&:hover': {
    backgroundColor: theme.palette.primary.dark,
    boxShadow: `0px 2px 4px -1px ${alpha(theme.palette.common.black, 0.2)},
                0px 4px 5px 0px ${alpha(theme.palette.common.black, 0.14)},
                0px 1px 10px 0px ${alpha(theme.palette.common.black, 0.12)}`,
  },
}));

export const SecondaryButton = styled(Button)(({ theme }) => ({
  backgroundColor: 'transparent',
  color: theme.palette.primary.main,
  borderRadius: theme.shape.borderRadius * 4, // Pill-shaped button
  padding: theme.spacing(1.5, 3),
  textTransform: 'none',
  fontWeight: 500,
  border: `1px solid ${theme.palette.primary.main}`,
  '&:hover': {
    backgroundColor: alpha(theme.palette.primary.main, 0.04),
  },
}));

// Enhanced text field with Material 3 styling
export const StyledTextField = styled(TextField)(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    borderRadius: theme.shape.borderRadius * 1.5, // Slightly rounded corners
    '& fieldset': {
      borderColor: alpha(theme.palette.text.primary, 0.23),
    },
    '&:hover fieldset': {
      borderColor: theme.palette.text.primary,
    },
    '&.Mui-focused fieldset': {
      borderColor: theme.palette.primary.main,
    },
  },
  '& .MuiInputLabel-root': {
    color: theme.palette.text.secondary,
  },
  '& .MuiInputBase-input': {
    color: theme.palette.text.primary,
  },
}));

// Updated form control and select components
export const StyledFormControl = styled(FormControl)(({ theme }) => ({
  width: '100%',
  marginBottom: theme.spacing(2),
}));

export const StyledInputLabel = styled(InputLabel)(({ theme }) => ({
  color: theme.palette.text.secondary,
  '&.Mui-focused': {
    color: theme.palette.primary.main,
  },
}));

export const StyledSelect = styled(Select<string>)(({ theme }) => ({
  borderRadius: theme.shape.borderRadius * 1.5, // Slightly rounded corners
  '& .MuiOutlinedInput-notchedOutline': {
    borderColor: alpha(theme.palette.text.primary, 0.23),
  },
  '&:hover .MuiOutlinedInput-notchedOutline': {
    borderColor: theme.palette.text.primary,
  },
  '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
    borderColor: theme.palette.primary.main,
  },
}));

// Layout components
export const ListContainer = styled(Box)(({ theme }) => ({
  marginTop: theme.spacing(4),
}));

export const SpaceBetweenBox = styled(Box)({
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
});

export const CenteredBox = styled(Box)({
  display: 'flex',
  justifyContent: 'center',
  alignItems: 'center',
});

// Progress indicator
export const StyledCircularProgress = styled(CircularProgress)(({ theme }) => ({
  color: theme.palette.primary.main,
}));

// Error text
export const ErrorText = styled(Typography)(({ theme }) => ({
  color: theme.palette.error.main,
  fontWeight: 500,
}));

// Alert component
export const StyledAlert = styled(Alert)(({ theme }) => ({
  marginBottom: theme.spacing(2),
  borderRadius: theme.shape.borderRadius * 1.5,
}));

// Form
export const StyledForm = styled('form')({
  width: '100%',
});

// Table components
export const StyledTableCell = styled(TableCell)(({ theme }) => ({
  borderBottom: `1px solid ${theme.palette.divider}`,
  color: theme.palette.text.primary,
  padding: theme.spacing(2),
}));

export const StyledTableRow = styled(TableRow)(({ theme }) => ({
  '&:nth-of-type(odd)': {
    backgroundColor: alpha(theme.palette.action.hover, 0.04),
  },
  '&:hover': {
    backgroundColor: alpha(theme.palette.action.hover, 0.08),
  },
}));

// New components aligned with Material Design 3

// Floating Action Button (FAB)
export const StyledFab = styled(Fab)(({ theme }) => ({
  position: 'fixed',
  bottom: theme.spacing(4),
  right: theme.spacing(4),
  borderRadius: '16px', // More rounded corners for FAB
}));

// App Bar
export const StyledAppBar = styled(AppBar)(({ theme }) => ({
  backgroundColor: alpha(theme.palette.background.paper, 0.8),
  backdropFilter: 'blur(20px)',
  color: theme.palette.text.primary,
  boxShadow: 'none',
  borderBottom: `1px solid ${theme.palette.divider}`,
}));

export const StyledToolbar = styled(Toolbar)(({ theme }) => ({
  justifyContent: 'space-between',
  padding: theme.spacing(0, 2),
  [theme.breakpoints.up('sm')]: {
    padding: theme.spacing(0, 3),
  },
}));

// Card component for list items
export const StyledCard = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(2),
  marginBottom: theme.spacing(2),
  borderRadius: theme.shape.borderRadius * 2,
  transition: theme.transitions.create(['box-shadow', 'transform']),
  '&:hover': {
    boxShadow: theme.shadows[4],
    transform: 'translateY(-2px)',
  },
}));

// Re-export Material-UI components that don't need custom styling
export { Table, TableBody, TableContainer, TableHead, TableRow, Paper, IconButton } from '@mui/material';
