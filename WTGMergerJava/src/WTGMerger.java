import net.moonlightflower.wc3libs.bin.app.WTG;
import net.moonlightflower.wc3libs.bin.Wc3BinOutputStream;
import java.io.*;

/**
 * Simple WTG testing tool - validates that wc3libs can read and write WTG files correctly.
 * This is a proof-of-concept to verify wc3libs works better than War3Net.
 */
public class WTGMerger {

    public static void main(String[] args) {
        System.out.println("╔═══════════════════════════════════════════════════════════╗");
        System.out.println("║         WTG Test Tool - wc3libs Proof of Concept         ║");
        System.out.println("╚═══════════════════════════════════════════════════════════╝");
        System.out.println();

        if (args.length < 2) {
            System.out.println("Usage: java WTGMerger <input.wtg> <output.wtg>");
            System.out.println();
            System.out.println("This tool reads a WTG file and writes it back using wc3libs.");
            System.out.println("If the output opens correctly in World Editor with all triggers/variables,");
            System.out.println("then wc3libs is working correctly (unlike War3Net).");
            System.out.println();
            System.out.println("Example:");
            System.out.println("  java WTGMerger input.wtg output.wtg");
            System.exit(1);
        }

        String inputPath = args[0];
        String outputPath = args[1];

        try {
            // Read WTG
            System.out.println("Reading: " + inputPath);
            File inputFile = new File(inputPath);
            WTG wtg = new WTG(inputFile);

            System.out.println("✓ WTG loaded successfully");
            System.out.println();
            System.out.println("Statistics:");
            System.out.println("  Variables:  " + wtg.getVars().size());
            System.out.println("  Triggers:   " + wtg.getTrigs().size());
            System.out.println("  Categories: " + wtg.getTrigCats().size());

            // Show sample variables
            if (!wtg.getVars().isEmpty()) {
                System.out.println();
                System.out.println("Sample variables:");
                int count = 0;
                for (WTG.Var var : wtg.getVars().values()) {
                    System.out.println("  - " + var.getName() + " (" + var.getType() + ")");
                    if (++count >= 10) {
                        if (wtg.getVars().size() > 10) {
                            System.out.println("  ... and " + (wtg.getVars().size() - 10) + " more");
                        }
                        break;
                    }
                }
            }

            // Write WTG
            System.out.println();
            System.out.println("Writing: " + outputPath);
            File outputFile = new File(outputPath);

            try (FileOutputStream fos = new FileOutputStream(outputFile);
                 Wc3BinOutputStream wos = new Wc3BinOutputStream(fos)) {
                wtg.write(wos);
            }

            System.out.println("✓ WTG written successfully");
            System.out.println("  Output size: " + outputFile.length() + " bytes");
            System.out.println();
            System.out.println("╔═══════════════════════════════════════════════════════════╗");
            System.out.println("║                       SUCCESS!                            ║");
            System.out.println("╚═══════════════════════════════════════════════════════════╝");
            System.out.println();
            System.out.println("Now test if the output WTG file opens correctly in World Editor.");
            System.out.println("If it does, wc3libs is working correctly.");

        } catch (Exception e) {
            System.err.println();
            System.err.println("❌ ERROR: " + e.getMessage());
            e.printStackTrace();
            System.exit(1);
        }
    }
}
