import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Docker image uses Next standalone server (Faz 11.1.2)
  output: "standalone",
};

export default nextConfig;
