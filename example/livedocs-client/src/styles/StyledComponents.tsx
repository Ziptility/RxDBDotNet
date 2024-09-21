// src\styles\StyledComponents.tsx
import {
  Box,
  Button,
  CircularProgress,
  Container,
  Paper,
  Typography,
  Alert,
  TextField,
  TableCell,
  TableRow,
  Card,
  CardContent,
  Chip,
} from '@mui/material';
import { styled, alpha } from '@mui/material/styles';

export const PageContainer = styled(Container)(({ theme }) => ({
  paddingTop: theme.spacing(4),
  paddingBottom: theme.spacing(4),
  paddingLeft: `calc(80px + ${theme.spacing(3)})`, // 80px for NavigationRail + some extra padding
  paddingRight: theme.spacing(3),
  [theme.breakpoints.up('sm')]: {
    paddingTop: theme.spacing(6),
    paddingBottom: theme.spacing(6),
  },
}));

export const ContentPaper = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(4),
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius * 2,
  boxShadow: theme.shadows[1],
  transition: theme.transitions.create(['box-shadow']),
  '&:hover': {
    boxShadow: theme.shadows[2],
  },
}));

export const PageTitle = styled(Typography)(({ theme }) => ({
  marginBottom: theme.spacing(4),
  color: theme.palette.primary.main,
  fontWeight: 700,
}));

export const SectionTitle = styled(Typography)(({ theme }) => ({
  marginBottom: theme.spacing(3),
  color: theme.palette.text.primary,
  fontWeight: 600,
}));

export const FormContainer = styled(Box)(({ theme }) => ({
  display: 'flex',
  flexDirection: 'column',
  gap: theme.spacing(3),
}));

export const PrimaryButton = styled(Button)(({ theme }) => ({
  borderRadius: theme.shape.borderRadius * 4,
  padding: theme.spacing(1.5, 3),
  textTransform: 'none',
  fontWeight: 500,
  fontSize: '1rem',
  lineHeight: 1.75,
  letterSpacing: '0.02857em',
}));

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

export const StyledCircularProgress = styled(CircularProgress)(({ theme }) => ({
  color: theme.palette.primary.main,
}));

export const ErrorText = styled(Typography)(({ theme }) => ({
  color: theme.palette.error.main,
  fontWeight: 500,
}));

export const StyledAlert = styled(Alert)(({ theme }) => ({
  marginBottom: theme.spacing(2),
  borderRadius: theme.shape.borderRadius * 2,
}));

export const StyledForm = styled('form')({
  width: '100%',
});

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

export const StyledCard = styled(Card)(({ theme }) => ({
  borderRadius: theme.shape.borderRadius * 3,
  transition: theme.transitions.create(['box-shadow', 'transform']),
  '&:hover': {
    boxShadow: theme.shadows[4],
    transform: 'translateY(-2px)',
  },
}));

export const StyledCardContent = styled(CardContent)(({ theme }) => ({
  padding: theme.spacing(3),
}));

export const StyledChip = styled(Chip)(({ theme }) => ({
  borderRadius: theme.shape.borderRadius,
  height: '32px',
  '& .MuiChip-label': {
    paddingLeft: theme.spacing(1.5),
    paddingRight: theme.spacing(1.5),
    fontSize: '0.875rem',
    fontWeight: 500,
  },
}));

export const SearchBar = styled(TextField)(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    borderRadius: '28px',
    backgroundColor: alpha(theme.palette.background.paper, 0.15),
    '&:hover': {
      backgroundColor: alpha(theme.palette.background.paper, 0.25),
    },
    '& fieldset': {
      borderColor: 'transparent',
    },
    '&:hover fieldset': {
      borderColor: 'transparent',
    },
    '&.Mui-focused fieldset': {
      borderColor: theme.palette.primary.main,
    },
  },
}));

export const ResponsiveGrid = styled(Box)(({ theme }) => ({
  display: 'grid',
  gap: theme.spacing(3),
  gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
}));

// Re-export Material-UI components that don't need custom styling
export {
  Table,
  TableBody,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  TextField,
  Select,
} from '@mui/material';
