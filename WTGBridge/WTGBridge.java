import net.moonlightflower.wc3libs.bin.app.WTG;
import net.moonlightflower.wc3libs.bin.Wc3BinOutputStream;
import java.io.*;

/**
 * Bridge between C# tools and wc3libs for WTG file operations.
 * Provides stable WTG writing using wc3libs Java library.
 */
public class WTGBridge {
    public static void main(String[] args) {
        if (args.length < 2) {
            System.err.println("Usage: java WTGBridge <command> <input> [output]");
            System.err.println("Commands:");
            System.err.println("  copy <input.wtg> <output.wtg> - Copy WTG file using wc3libs (re-serialize)");
            System.err.println("  validate <input.wtg> - Validate WTG file");
            System.exit(1);
        }

        String command = args[0];
        String inputPath = args[1];

        try {
            switch (command) {
                case "copy":
                    if (args.length < 3) {
                        System.err.println("Error: output path required for copy command");
                        System.exit(1);
                    }
                    String outputPath = args[2];
                    copyWTG(inputPath, outputPath);
                    break;
                    
                case "validate":
                    validateWTG(inputPath);
                    break;
                    
                default:
                    System.err.println("Unknown command: " + command);
                    System.exit(1);
            }
        } catch (Exception e) {
            System.err.println("Error: " + e.getMessage());
            e.printStackTrace();
            System.exit(1);
        }
    }

    private static void copyWTG(String inputPath, String outputPath) throws Exception {
        System.out.println("Reading WTG from: " + inputPath);
        File inputFile = new File(inputPath);

        // Read using wc3libs
        WTG wtg = new WTG(inputFile);

        System.out.println("WTG loaded successfully");
        System.out.println("  Variables: " + wtg.getVars().size());
        System.out.println("  Triggers: " + wtg.getTrigs().size());
        System.out.println("  Categories: " + wtg.getTrigCats().size());

        // Write using wc3libs
        System.out.println("Writing WTG to: " + outputPath);
        File outputFile = new File(outputPath);

        try (FileOutputStream fos = new FileOutputStream(outputFile);
             Wc3BinOutputStream wos = new Wc3BinOutputStream(fos)) {
            wtg.write(wos);
        }

        System.out.println("WTG written successfully");
        System.out.println("  Output size: " + outputFile.length() + " bytes");
    }

    private static void validateWTG(String inputPath) throws Exception {
        System.out.println("Validating WTG: " + inputPath);
        File inputFile = new File(inputPath);

        WTG wtg = new WTG(inputFile);

        System.out.println("WTG is valid");
        System.out.println("  Variables: " + wtg.getVars().size());
        System.out.println("  Triggers: " + wtg.getTrigs().size());
        System.out.println("  Categories: " + wtg.getTrigCats().size());

        // List first few variables
        int count = 0;
        for (WTG.Var var : wtg.getVars().values()) {
            System.out.println("    " + var.getName() + " (" + var.getType() + ")");
            if (++count >= 10) {
                if (wtg.getVars().size() > 10) {
                    System.out.println("    ... and " + (wtg.getVars().size() - 10) + " more");
                }
                break;
            }
        }
    }
}
