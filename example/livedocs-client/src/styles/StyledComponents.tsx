import { Box, Paper, Typography, Button, TextField, Alert, CircularProgress, Select } from '@mui/material';
import { styled } from '@mui/material/styles';
import type {
  BoxProps,
  PaperProps,
  TypographyProps,
  ButtonProps,
  TextFieldProps,
  AlertProps,
  CircularProgressProps,
  SelectProps,
} from '@mui/material';

export const PageContainer = styled(Box)<BoxProps>(({ theme }) => ({
  padding: theme.spacing(3),
  maxWidth: '1200px',
  margin: '0 auto',
  backgroundColor: theme.palette.background.default,
}));

export const ContentPaper = styled(Paper)<PaperProps>(({ theme }) => ({
  padding: theme.spacing(3),
  marginBottom: theme.spacing(3),
  backgroundColor: theme.palette.background.paper,
  boxShadow: theme.shadows[1],
}));

export const PageTitle = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.text.primary,
  fontWeight: 'bold',
  marginBottom: theme.spacing(2),
}));

export const SectionTitle = styled(Typography)<TypographyProps>(({ theme }) => ({
  color: theme.palette.text.secondary,
  fontWeight: 'bold',
  marginBottom: theme.spacing(2),
}));

export const FormContainer = styled(Box)<BoxProps>(({ theme }) => ({
  display: 'flex',
  flexDirection: 'column',
  gap: theme.spacing(2),
}));

export const StyledTextField = styled(TextField)<TextFieldProps>(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    '& fieldset': {
      borderColor: theme.palette.secondary.light,
    },
    '&:hover fieldset': {
      borderColor: theme.palette.secondary.main,
    },
    '&.Mui-focused fieldset': {
      borderColor: theme.palette.primary.main,
    },
  },
}));

export const PrimaryButton = styled(Button)<ButtonProps>(({ theme }) => ({
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  '&:hover': {
    backgroundColor: theme.palette.primary.dark,
  },
}));

export const SecondaryButton = styled(Button)<ButtonProps>(({ theme }) => ({
  backgroundColor: theme.palette.secondary.main,
  color: theme.palette.secondary.contrastText,
  '&:hover': {
    backgroundColor: theme.palette.secondary.dark,
  },
}));

export const ListContainer = styled(Box)<BoxProps>(({ theme }) => ({
  marginTop: theme.spacing(3),
}));

export const FlexBox = styled(Box)<BoxProps>({
  display: 'flex',
});

export const CenteredBox = styled(Box)<BoxProps & { height?: string }>(({ height }) => ({
  display: 'flex',
  justifyContent: 'center',
  alignItems: 'center',
  height: height ?? 'auto',
}));

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

export const RoundedBox = styled(Box)<BoxProps>(({ theme }) => ({
  borderRadius: theme.shape.borderRadius,
}));

export const StyledForm = styled('form')(({ theme }) => ({
  width: '100%',
  marginTop: theme.spacing(1),
}));

export const StyledAlert = styled(Alert)<AlertProps>(({ theme }) => ({
  marginBottom: theme.spacing(2),
}));

export const StyledCircularProgress = styled(CircularProgress)<CircularProgressProps>(({ theme }) => ({
  color: theme.palette.primary.main,
}));

export const StyledSelect = styled(Select<string>)<SelectProps<string>>((props) => ({
  '& .MuiOutlinedInput-notchedOutline': {
    borderColor: props.theme.palette.secondary.light,
  },
  '&:hover .MuiOutlinedInput-notchedOutline': {
    borderColor: props.theme.palette.secondary.main,
  },
  '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
    borderColor: props.theme.palette.primary.main,
  },
}));
