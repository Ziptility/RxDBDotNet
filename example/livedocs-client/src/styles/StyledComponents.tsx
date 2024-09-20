import { Box, Paper, Typography, Alert, Button, TextField, CircularProgress, Select } from '@mui/material';
import { styled } from '@mui/material/styles';
import type {
  BoxProps,
  PaperProps,
  TypographyProps,
  AlertProps,
  ButtonProps,
  TextFieldProps,
  SelectProps,
} from '@mui/material';

export const PageContainer = styled(Box)<BoxProps>(({ theme }) => ({
  padding: theme.spacing(3),
  maxWidth: '1000px',
  margin: '0 auto',
  backgroundColor: theme.palette.background.default,
}));

export const ContentPaper = styled(Paper)<PaperProps>(({ theme }) => ({
  padding: theme.spacing(3),
  marginBottom: theme.spacing(3),
  backgroundColor: theme.palette.background.paper,
  boxShadow: 'none',
  border: `1px solid ${theme.palette.divider}`,
}));

export const PageTitle = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.primary.main,
  marginBottom: theme.spacing(3),
  fontWeight: 500,
}));

export const SectionTitle = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.text.secondary,
  marginBottom: theme.spacing(2),
  fontWeight: 500,
}));

export const FormContainer = styled(Box)<BoxProps>(({ theme }) => ({
  display: 'flex',
  flexDirection: 'column',
  gap: theme.spacing(2),
}));

export const ListContainer = styled(Box)<BoxProps>(({ theme }) => ({
  marginTop: theme.spacing(3),
}));

export const FlexBox = styled(Box)<BoxProps>({
  display: 'flex',
});

export const CenteredBox = styled(Box)<BoxProps>({
  display: 'flex',
  justifyContent: 'center',
  alignItems: 'center',
});

export const SpaceBetweenBox = styled(Box)<BoxProps>({
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
});

export const ErrorText = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.error.main,
}));

export const SuccessText = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.success.main,
}));

export const WarningText = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.warning.main,
}));

export const InfoText = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.info.main,
}));

export const StyledForm = styled('form')(({ theme }) => ({
  width: '100%',
  marginTop: theme.spacing(2),
}));

export const StyledAlert = styled(Alert)<AlertProps>(({ theme }) => ({
  marginBottom: theme.spacing(2),
  backgroundColor: theme.palette.background.paper,
  color: theme.palette.text.primary,
  '& .MuiAlert-icon': {
    color: theme.palette.primary.main,
  },
}));

export const PrimaryButton = styled(Button)<ButtonProps>(({ theme }) => ({
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  padding: theme.spacing(1, 2),
  '&:hover': {
    backgroundColor: theme.palette.primary.dark,
  },
  '&:disabled': {
    backgroundColor: theme.palette.action.disabledBackground,
    color: theme.palette.action.disabled,
  },
}));

export const SecondaryButton = styled(Button)<ButtonProps>(({ theme }) => ({
  backgroundColor: 'transparent',
  color: theme.palette.primary.main,
  padding: theme.spacing(1, 2),
  border: `1px solid ${theme.palette.primary.main}`,
  '&:hover': {
    backgroundColor: theme.palette.action.hover,
  },
  '&:disabled': {
    backgroundColor: 'transparent',
    color: theme.palette.action.disabled,
    borderColor: theme.palette.action.disabled,
  },
}));

export const StyledTextField = styled(TextField)<TextFieldProps>(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    '& fieldset': {
      borderColor: theme.palette.divider,
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
  },
  '& .MuiInputBase-input': {
    color: theme.palette.text.primary,
  },
}));

export const StyledSelect = styled(Select<string>)<SelectProps<string>>(({ theme }) => ({
  '& .MuiOutlinedInput-notchedOutline': {
    borderColor: theme.palette.divider,
  },
  '&:hover .MuiOutlinedInput-notchedOutline': {
    borderColor: theme.palette.primary.main,
  },
  '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
    borderColor: theme.palette.primary.main,
  },
  '& .MuiSelect-select': {
    color: theme.palette.text.primary,
  },
}));

export const StyledCircularProgress = styled(CircularProgress)(({ theme }) => ({
  color: theme.palette.primary.main,
}));

// Re-export Material-UI components that don't need custom styling
export { CircularProgress };
