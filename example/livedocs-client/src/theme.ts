// src\theme.ts
import {
  argbFromHex,
  themeFromSourceColor,
  applyTheme,
  type Theme as MaterialColorTheme,
} from '@material/material-color-utilities';
import { createTheme, type ThemeOptions } from '@mui/material/styles';

// Function to convert ARGB to hex
const hexFromArgb = (argb: number): string => {
  const r = (argb >> 16) & 255;
  const g = (argb >> 8) & 255;
  const b = argb & 255;
  return `#${[r, g, b].map((x) => x.toString(16).padStart(2, '0')).join('')}`;
};

// Function to generate Material You theme
const generateMaterialYouTheme = (sourceColor: string): MaterialColorTheme => {
  const theme = themeFromSourceColor(argbFromHex(sourceColor));
  return theme;
};

// Function to create MUI theme from Material You theme
const createMuiThemeFromMaterialYou = (materialTheme: MaterialColorTheme, isDark = true): ThemeOptions => {
  const scheme = isDark ? materialTheme.schemes.dark : materialTheme.schemes.light;

  return {
    palette: {
      mode: isDark ? 'dark' : 'light',
      primary: {
        main: hexFromArgb(scheme.primary),
        light: hexFromArgb(materialTheme.palettes.primary.tone(80)),
        dark: hexFromArgb(materialTheme.palettes.primary.tone(20)),
      },
      secondary: {
        main: hexFromArgb(scheme.secondary),
        light: hexFromArgb(materialTheme.palettes.secondary.tone(80)),
        dark: hexFromArgb(materialTheme.palettes.secondary.tone(20)),
      },
      background: {
        default: hexFromArgb(scheme.background),
        paper: hexFromArgb(scheme.surface),
      },
      text: {
        primary: hexFromArgb(scheme.onBackground),
        secondary: hexFromArgb(scheme.onSurfaceVariant),
      },
      error: {
        main: hexFromArgb(scheme.error),
      },
      warning: {
        main: hexFromArgb(materialTheme.palettes.tertiary.tone(40)),
      },
      info: {
        main: hexFromArgb(materialTheme.palettes.tertiary.tone(40)),
      },
      success: {
        main: hexFromArgb(materialTheme.palettes.tertiary.tone(40)),
      },
    },
    typography: {
      fontFamily: 'Inter, Roboto, "Helvetica Neue", Arial, sans-serif',
      h1: {
        fontSize: '3.5625rem', // 57px
        lineHeight: 1.12,
        letterSpacing: '-0.015625rem', // -0.25px
        fontWeight: 400,
      },
      h2: {
        fontSize: '2.8125rem', // 45px
        lineHeight: 1.15,
        letterSpacing: 0,
        fontWeight: 400,
      },
      h3: {
        fontSize: '2.25rem', // 36px
        lineHeight: 1.22,
        letterSpacing: 0,
        fontWeight: 400,
      },
      h4: {
        fontSize: '2rem', // 32px
        lineHeight: 1.25,
        letterSpacing: 0,
        fontWeight: 400,
      },
      h5: {
        fontSize: '1.75rem', // 28px
        lineHeight: 1.29,
        letterSpacing: 0,
        fontWeight: 400,
      },
      h6: {
        fontSize: '1.5rem', // 24px
        lineHeight: 1.33,
        letterSpacing: 0,
        fontWeight: 400,
      },
      subtitle1: {
        fontSize: '1rem', // 16px
        lineHeight: 1.5,
        letterSpacing: '0.009375rem', // 0.15px
        fontWeight: 500,
      },
      subtitle2: {
        fontSize: '0.875rem', // 14px
        lineHeight: 1.57,
        letterSpacing: '0.00625rem', // 0.1px
        fontWeight: 500,
      },
      body1: {
        fontSize: '1rem', // 16px
        lineHeight: 1.5,
        letterSpacing: '0.03125rem', // 0.5px
        fontWeight: 400,
      },
      body2: {
        fontSize: '0.875rem', // 14px
        lineHeight: 1.43,
        letterSpacing: '0.015625rem', // 0.25px
        fontWeight: 400,
      },
      button: {
        fontSize: '0.875rem', // 14px
        lineHeight: 1.75,
        letterSpacing: '0.025rem', // 0.4px
        fontWeight: 500,
        textTransform: 'uppercase',
      },
      caption: {
        fontSize: '0.75rem', // 12px
        lineHeight: 1.66,
        letterSpacing: '0.025rem', // 0.4px
        fontWeight: 400,
      },
      overline: {
        fontSize: '0.75rem', // 12px
        lineHeight: 2.66,
        letterSpacing: '0.0625rem', // 1px
        fontWeight: 400,
        textTransform: 'uppercase',
      },
    },
    shape: {
      borderRadius: 16,
    },
    components: {
      MuiButton: {
        styleOverrides: {
          root: {
            textTransform: 'none',
            borderRadius: '20px',
            padding: '10px 24px',
          },
          contained: {
            boxShadow: 'none',
            '&:hover': {
              boxShadow:
                '0px 2px 4px -1px rgba(0,0,0,0.2), 0px 4px 5px 0px rgba(0,0,0,0.14), 0px 1px 10px 0px rgba(0,0,0,0.12)',
            },
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
          },
          rounded: {
            borderRadius: '16px',
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: '24px',
            boxShadow:
              '0px 2px 4px -1px rgba(0,0,0,0.2), 0px 4px 5px 0px rgba(0,0,0,0.14), 0px 1px 10px 0px rgba(0,0,0,0.12)',
          },
        },
      },
      MuiTextField: {
        styleOverrides: {
          root: {
            '& .MuiOutlinedInput-root': {
              borderRadius: '12px',
            },
          },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: '8px',
          },
        },
      },
      MuiTooltip: {
        styleOverrides: {
          tooltip: {
            borderRadius: '8px',
          },
        },
      },
    },
  };
};

// Generate Material You theme
const materialYouTheme = generateMaterialYouTheme('#673AB7'); // Deep Purple as the primary color

// Create MUI theme
export const theme = createTheme(createMuiThemeFromMaterialYou(materialYouTheme, true));

// Apply theme to document
if (typeof document !== 'undefined') {
  applyTheme(materialYouTheme, { target: document.body, dark: true });
}
