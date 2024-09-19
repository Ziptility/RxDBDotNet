// src\pages\_document.tsx
import createEmotionServer from '@emotion/server/create-instance';
import { type AppProps } from 'next/app';
import type { AppPropsType } from 'next/dist/shared/lib/utils';
import Document, { Html, Head, Main, NextScript, type DocumentContext, type DocumentInitialProps } from 'next/document';
import { createEmotionCache } from '../createEmotionCache';
import { theme } from '../theme';
import type { EmotionCache } from '@emotion/cache';

interface MyDocumentProps extends DocumentInitialProps {
  emotionStyleTags: JSX.Element[];
}

interface EnhancedAppProps extends AppProps {
  emotionCache?: EmotionCache;
}

type PageProps = AppPropsType['pageProps'];

class MyDocument extends Document<MyDocumentProps> {
  override render = (): JSX.Element => {
    return (
      <Html lang="en">
        <Head>
          <meta name="theme-color" content={theme.palette.primary.main} />
          <link rel="stylesheet" href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" />
          {this.props.emotionStyleTags}
        </Head>
        <body>
          <Main />
          <NextScript />
        </body>
      </Html>
    );
  };

  static override async getInitialProps(ctx: DocumentContext): Promise<MyDocumentProps> {
    const originalRenderPage = ctx.renderPage;
    const cache = createEmotionCache();
    const emotionServer = createEmotionServer(cache);

    ctx.renderPage = (): ReturnType<typeof ctx.renderPage> =>
      originalRenderPage({
        enhanceApp: (App: React.ComponentType<EnhancedAppProps>) =>
          function EnhanceApp(props: EnhancedAppProps): JSX.Element {
            return (
              <App
                Component={props.Component}
                pageProps={props.pageProps as PageProps}
                router={props.router}
                emotionCache={cache}
              />
            );
          },
      });

    const initialProps = await Document.getInitialProps(ctx);
    const emotionStyles = emotionServer.extractCriticalToChunks(initialProps.html);
    const emotionStyleTags = emotionStyles.styles.map((style) => (
      <style data-emotion={`${style.key} ${style.ids.join(' ')}`} key={style.key} suppressHydrationWarning>
        {style.css}
      </style>
    ));

    return {
      ...initialProps,
      emotionStyleTags,
    };
  }
}

export default MyDocument;
