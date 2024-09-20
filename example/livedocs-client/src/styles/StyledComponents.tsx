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
} from '@mui/material';
import { styled } from '@mui/material/styles';

export const PageContainer = styled(Container)(({ theme }) => ({
  paddingTop: theme.spacing(4),
  paddingBottom: theme.spacing(4),
}));

export const ContentPaper = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(3),
  backgroundColor: theme.palette.background.paper,
  borderRadius: theme.shape.borderRadius,
}));

export const PageTitle = styled(Typography)(({ theme }) => ({
  marginBottom: theme.spacing(3),
  color: theme.palette.primary.main,
}));

export const SectionTitle = styled(Typography)(({ theme }) => ({
  marginBottom: theme.spacing(2),
  color: theme.palette.text.primary,
}));

export const FormContainer = styled(Box)(({ theme }) => ({
  display: 'flex',
  flexDirection: 'column',
  gap: theme.spacing(2),
}));

export const PrimaryButton = styled(Button)(({ theme }) => ({
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  '&:hover': {
    backgroundColor: theme.palette.primary.dark,
  },
  '&.Mui-disabled': {
    backgroundColor: theme.palette.action.disabledBackground,
    color: theme.palette.action.disabled,
  },
}));

export const SecondaryButton = styled(Button)(({ theme }) => ({
  backgroundColor: 'transparent',
  color: theme.palette.primary.main,
  border: `1px solid ${theme.palette.primary.main}`,
  '&:hover': {
    backgroundColor: theme.palette.action.hover,
  },
}));

export const StyledTextField = styled(TextField)(({ theme }) => ({
  marginBottom: theme.spacing(2),
  '& .MuiOutlinedInput-root': {
    '& fieldset': {
      borderColor: theme.palette.text.secondary,
    },
    '&:hover fieldset': {
      borderColor: theme.palette.primary.main,
    },
    '&.Mui-focused fieldset': {
      borderColor: theme.palette.primary.main,
    },
  },
  '& .MuiInputLabel-root': {
    color: theme.palette.text.secondary,
    '&.Mui-focused': {
      color: theme.palette.primary.main,
    },
  },
  '& .MuiInputBase-input': {
    color: theme.palette.text.primary,
  },
}));

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
  '& .MuiOutlinedInput-notchedOutline': {
    borderColor: theme.palette.text.secondary,
  },
  '&:hover .MuiOutlinedInput-notchedOutline': {
    borderColor: theme.palette.primary.main,
  },
  '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
    borderColor: theme.palette.primary.main,
  },
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
}));

export const StyledAlert = styled(Alert)(({ theme }) => ({
  marginBottom: theme.spacing(2),
}));

export const StyledForm = styled('form')({
  width: '100%',
});

// Re-export Material-UI components that don't need custom styling
export { TextField } from '@mui/material';
