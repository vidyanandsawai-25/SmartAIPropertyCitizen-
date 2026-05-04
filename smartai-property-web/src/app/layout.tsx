import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "SmartAI - मालमत्ता कर सहाय्यक",
  description: "अकोला महानगरपालिका - मालमत्ता कर सहाय्यक",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="mr">
      <body>{children}</body>
    </html>
  );
}
