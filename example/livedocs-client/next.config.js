const nextConfig = {
  reactStrictMode: true,
  webpack: (config, { dev, isServer }) => {
    return config;
  },
  eslint: {
    dirs: ['src'],
    ignoreDuringBuilds: false,
  },
  typescript: {
    ignoreBuildErrors: false,
  },
  poweredByHeader: false,
};

module.exports = nextConfig;
