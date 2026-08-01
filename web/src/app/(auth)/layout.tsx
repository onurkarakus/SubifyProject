import { SubifyLogo } from "@/components/brand/logo";
import Link from "next/link";

export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="flex min-h-screen flex-col bg-aurora">
      <div className="flex items-center justify-center px-4 pt-8">
        <Link href="/">
          <SubifyLogo />
        </Link>
      </div>
      <div className="flex flex-1 items-center justify-center px-4 py-10">
        <div className="w-full max-w-md">{children}</div>
      </div>
    </div>
  );
}
