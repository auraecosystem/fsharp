/* web-ai-domainprep.vala - Web4.AI Domain Sanitizer & IDNA Resolver */

using ICU;

namespace web4.AI {

    public class DomainResolver : GLib.Object {

        public static string to_ascii_domain (string input_domain) throws GLib.Error {
            ErrorCode status = ErrorCode.ZERO_ERROR;
            
            // Open UTS #46 IDNA engine (Non-transitional processing for modern Web4 domains)
            IDNA? idna = IDNA.open_uts46(0, out status);

            if (status.is_failure() || idna == null) {
                throw new GLib.IOError.FAILED("ICU UTS46 Initialization Failed: %s".printf(status.to_string()));
            }

            uint8[] dest = new uint8[256];
            IDNAInfo info = IDNAInfo();

            int len = idna.name_to_ascii_utf8(
                input_domain.data,
                input_domain.length,
                dest,
                dest.length - 1,
                out info,
                out status
            );

            // Check for ICU status failure or non-zero IDNA error bitmask
            if (status.is_failure() || info.errors != 0) {
                throw new GLib.IOError.INVALID_DATA(
                    "IDNA validation failed for '%s': %s (Error Bitmask: 0x%X)".printf(
                        input_domain, status.to_string(), info.errors
                    )
                );
            }

            // Guarantee explicit null-termination before string casting
            dest[len] = 0;
            return (string) dest;
        }

        public static int main (string[] args) {
            // Target internationalized Web4 AI node handle
            string target_handle = (args.length > 1) ? args[1] : "münchen.node.web4.ai";

            stdout.printf("========================================\n");
            stdout.printf("  Web4.AI Node Domain Sanitizer Tool   \n");
            stdout.printf("========================================\n");
            stdout.printf("Input Node Handle : %s\n", target_handle);

            try {
                string ascii_domain = to_ascii_domain(target_handle);
                
                stdout.printf("Punycode / ASCII  : %s\n", ascii_domain);
                stdout.printf("Agent Endpoint    : https://%s/v1/inference\n", ascii_domain);
                stdout.printf("Status            : VALIDATED\n");
            } catch (GLib.Error e) {
                stderr.printf("Web4.AI Resolution Error: %s\n", e.message);
                return 1;
            }

            return 0;
        }
