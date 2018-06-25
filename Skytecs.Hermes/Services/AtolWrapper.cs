using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Services
{
    public class AtolWrapper : IDisposable
    {
        private IntPtr _fptr;
        private int _deviceId;

        public AtolWrapper(IOptions<FiscalPrinterSettings> config)
            : this(config.Value.Port, config.Value.DeviceId)
        {
        }

        public AtolWrapper(int portNumber, int deviceId)
        {
            _deviceId = deviceId;

            libfptr_create(ref _fptr);
            libfptr_set_single_setting(_fptr, LIBFPTR_SETTING_PORT, port.LIBFPTR_PORT_COM.ToString());
            libfptr_set_single_setting(_fptr, LIBFPTR_SETTING_COM_FILE, portNumber.ToString());
            libfptr_set_single_setting(_fptr, LIBFPTR_SETTING_BAUDRATE, baudrate.LIBFPTR_PORT_BR_115200.ToString());
            libfptr_apply_single_settings(_fptr);
        }

        public void Open()
        {
            var result = libfptr_open(_fptr);
            if (result < 0)
            {
                (var code, var message) = GetLastError();
                throw new InvalidOperationException($"{code} - {message}");
            }
        }

        public (int, string) GetLastError()
        {
            var value = new StringBuilder(2048);
            var errorCode = libfptr_error_code(_fptr);
            var errorDescription = libfptr_error_description(_fptr, value, value.Capacity);
            return (errorCode, value.ToString());
        }

        public bool IsOpened()
        {
            return libfptr_is_opened(_fptr) == 1;
        }

        public void SetParam(int param, string value)
        {
            libfptr_set_param_str(_fptr, param, value);
        }

        public void OperatorLogin()
        {
            var result = libfptr_operator_login(_fptr);
        }

        public string GetSettings()
        {
            var settings = new StringBuilder(2048);
            libfptr_get_settings(_fptr, settings, settings.Capacity);
            return settings.ToString();
        }

        public void OpenShift()
        {
            if (libfptr_open_shift(_fptr) < 0)
            {
                (var code, var message) = GetLastError();
                throw new InvalidOperationException($"{code} - {message}");
            }
        }

        public void ExecuteCommand<TCommand>(TCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var ser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            ser.Converters.Add(new StringEnumConverter { CamelCaseText = true });

            string json;
            using (var writer = new StringWriter())
            {
                
                ser.Serialize(writer, command);

                json = writer.ToString();
            }

            //log json

            libfptr_set_param_str(_fptr, (int)param.LIBFPTR_PARAM_JSON_DATA, json);
            var status = libfptr_process_json(_fptr);
            if(status < 0)
            {
                (var code, var message) = GetLastError();
                throw new InvalidOperationException($"{code} - {message}");
            }

            /*
            var result = new StringBuilder(1024);


            var size = libfptr_get_param_str(_fptr, (int)param.LIBFPTR_PARAM_JSON_DATA, result, 1024);

            if (size > result.Capacity)
            {
                result.Capacity = size;
                libfptr_get_param_str(_fptr, (int)param.LIBFPTR_PARAM_JSON_DATA, result, result.Capacity);
            }
            //log result

            using (var stringReader = new StringReader(result.ToString()))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                return ser.Deserialize<TCommandResponse>(jsonReader);
            }*/
        }

        public void CheckDocumentClosed()
        {
            libfptr_check_document_closed(_fptr);
        }

        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
            if (_fptr != IntPtr.Zero)
            {
                libfptr_destroy(ref _fptr);
            }

            _fptr = IntPtr.Zero;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~AtolWrapper()
        {
            ReleaseUnmanagedResources();
        }

        #endregion


        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_create(ref IntPtr fptr);


        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_destroy(ref IntPtr fptr);

        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_set_single_setting(ref IntPtr fptr);


        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_set_param_str(IntPtr fptr, int param, string value);

        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_operator_login(IntPtr fptr);

        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_open_shift(IntPtr fptr);

        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_check_document_closed(IntPtr fptr);

        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_get_settings(IntPtr fptr, [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] out string settings, out int size);

        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_open(IntPtr fptr);

        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_error_code(IntPtr fptr);

        //[DllImport("fptr10.dll")]
        //private static extern int libfptr_is_opened(IntPtr fptr);

        enum error
        {
            LIBFPTR_OK = 0,
            LIBFPTR_ERROR_CONNECTION_DISABLED,
            LIBFPTR_ERROR_NO_CONNECTION,
            LIBFPTR_ERROR_PORT_BUSY,
            LIBFPTR_ERROR_PORT_NOT_AVAILABLE,
            LIBFPTR_ERROR_INCORRECT_DATA,
            LIBFPTR_ERROR_INTERNAL,
            LIBFPTR_ERROR_UNSUPPORTED_CAST,
            LIBFPTR_ERROR_NO_REQUIRED_PARAM,
            LIBFPTR_ERROR_INVALID_SETTINGS,
            LIBFPTR_ERROR_NOT_CONFIGURED,
            LIBFPTR_ERROR_NOT_SUPPORTED,
            LIBFPTR_ERROR_INVALID_MODE,
            LIBFPTR_ERROR_INVALID_PARAM,
            LIBFPTR_ERROR_NOT_LOADED,
            LIBFPTR_ERROR_UNKNOWN,

            LIBFPTR_ERROR_INVALID_SUM,
            LIBFPTR_ERROR_INVALID_QUANTITY,
            LIBFPTR_ERROR_CASH_COUNTER_OVERFLOW,
            LIBFPTR_ERROR_LAST_OPERATION_STORNO_DENIED,
            LIBFPTR_ERROR_STORNO_BY_CODE_DENIED,
            LIBFPTR_ERROR_LAST_OPERATION_NOT_REPEATABLE,
            LIBFPTR_ERROR_DISCOUNT_NOT_REPEATABLE,
            LIBFPTR_ERROR_DISCOUNT_DENIED,
            LIBFPTR_ERROR_INVALID_COMMODITY_CODE,
            LIBFPTR_ERROR_INVALID_COMMODITY_BARCODE,
            LIBFPTR_ERROR_INVALID_COMMAND_FORMAT,
            LIBFPTR_ERROR_INVALID_COMMAND_LENGTH,
            LIBFPTR_ERROR_BLOCKED_IN_DATE_INPUT_MODE,
            LIBFPTR_ERROR_NEED_DATE_ACCEPT,
            LIBFPTR_ERROR_NO_MORE_DATA,
            LIBFPTR_ERROR_NO_ACCEPT_OR_CANCEL,
            LIBFPTR_ERROR_BLOCKED_BY_REPORT_INTERRUPTION,
            LIBFPTR_ERROR_DISABLE_CASH_CONTROL_DENIED,
            LIBFPTR_ERROR_MODE_BLOCKED,
            LIBFPTR_ERROR_CHECK_DATE_TIME,
            LIBFPTR_ERROR_DATE_TIME_LESS_THAN_FS,
            LIBFPTR_ERROR_CLOSE_ARCHIVE_DENIED,
            LIBFPTR_ERROR_COMMODITY_NOT_FOUND,
            LIBFPTR_ERROR_WEIGHT_BARCODE_WITH_INVALID_QUANTITY,
            LIBFPTR_ERROR_RECEIPT_BUFFER_OVERFLOW,
            LIBFPTR_ERROR_QUANTITY_TOO_FEW,
            LIBFPTR_ERROR_STORNO_TOO_MUCH,
            LIBFPTR_ERROR_BLOCKED_COMMODITY_NOT_FOUND,
            LIBFPTR_ERROR_NO_PAPER,
            LIBFPTR_ERROR_COVER_OPENED,
            LIBFPTR_ERROR_PRINTER_FAULT,
            LIBFPTR_ERROR_MECHANICAL_FAULT,
            LIBFPTR_ERROR_INVALID_RECEIPT_TYPE,
            LIBFPTR_ERROR_INVALID_UNIT_TYPE,
            LIBFPTR_ERROR_NO_MEMORY,
            LIBFPTR_ERROR_PICTURE_NOT_FOUND,
            LIBFPTR_ERROR_NONCACH_PAYMENTS_TOO_MUCH,
            LIBFPTR_ERROR_RETURN_DENIED,
            LIBFPTR_ERROR_PAYMENTS_OVERFLOW,
            LIBFPTR_ERROR_BUSY,
            LIBFPTR_ERROR_GSM,
            LIBFPTR_ERROR_INVALID_DISCOUNT,
            LIBFPTR_ERROR_OPERATION_AFTER_DISCOUNT_DENIED,
            LIBFPTR_ERROR_INVALID_DEPARTMENT,
            LIBFPTR_ERROR_INVALID_PAYMENT_TYPE,
            LIBFPTR_ERROR_MULTIPLICATION_OVERFLOW,
            LIBFPTR_ERROR_DENIED_BY_SETTINGS,
            LIBFPTR_ERROR_TOTAL_OVERFLOW,
            LIBFPTR_ERROR_DENIED_IN_ANNULATION_RECEIPT,
            LIBFPTR_ERROR_JOURNAL_OVERFLOW,
            LIBFPTR_ERROR_NOT_FULLY_PAID,
            LIBFPTR_ERROR_DENIED_IN_RETURN_RECEIPT,
            LIBFPTR_ERROR_SHIFT_EXPIRED,
            LIBFPTR_ERROR_DENIED_IN_SELL_RECEIPT,
            LIBFPTR_ERROR_FISCAL_MEMORY_OVERFLOW,
            LIBFPTR_ERROR_INVALID_PASSWORD,
            LIBFPTR_ERROR_JOURNAL_BUSY,
            LIBFPTR_ERROR_DENIED_IN_CLOSED_SHIFT,
            LIBFPTR_ERROR_INVALID_TABLE_NUMBER,
            LIBFPTR_ERROR_INVALID_ROW_NUMBER,
            LIBFPTR_ERROR_INVALID_FIELD_NUMBER,
            LIBFPTR_ERROR_INVALID_DATE_TIME,
            LIBFPTR_ERROR_INVALID_STORNO_SUM,
            LIBFPTR_ERROR_CHANGE_CALCULATION,
            LIBFPTR_ERROR_NO_CASH,
            LIBFPTR_ERROR_DENIED_IN_CLOSED_RECEIPT,
            LIBFPTR_ERROR_DENIED_IN_OPENED_RECEIPT,
            LIBFPTR_ERROR_DENIED_IN_OPENED_SHIFT,
            LIBFPTR_ERROR_SERIAL_NUMBER_ALREADY_ENTERED,
            LIBFPTR_ERROR_TOO_MUCH_REREGISTRATIONS,
            LIBFPTR_ERROR_INVALID_SHIFT_NUMBER,
            LIBFPTR_ERROR_INVALID_SERIAL_NUMBER,
            LIBFPTR_ERROR_INVALID_RNM_VATIN,
            LIBFPTR_ERROR_FISCAL_PRINTER_NOT_ACTIVATED,
            LIBFPTR_ERROR_SERIAL_NUMBER_NOT_ENTERED,
            LIBFPTR_ERROR_NO_MORE_REPORTS,
            LIBFPTR_ERROR_MODE_NOT_ACTIVATED,
            LIBFPTR_ERROR_RECORD_NOT_FOUND_IN_JOURNAL,
            LIBFPTR_ERROR_INVALID_LICENSE,
            LIBFPTR_ERROR_NEED_FULL_RESET,
            LIBFPTR_ERROR_DENIED_BY_LICENSE,
            LIBFPTR_ERROR_DISCOUNT_CANCELLATION_DENIED,
            LIBFPTR_ERROR_CLOSE_RECEIPT_DENIED,
            LIBFPTR_ERROR_INVALID_ROUTE_NUMBER,
            LIBFPTR_ERROR_INVALID_START_ZONE_NUMBER,
            LIBFPTR_ERROR_INVALID_END_ZONE_NUMBER,
            LIBFPTR_ERROR_INVALID_RATE_TYPE,
            LIBFPTR_ERROR_INVALID_RATE,
            LIBFPTR_ERROR_FISCAL_MODULE_EXCHANGE,
            LIBFPTR_ERROR_NEED_TECHNICAL_SUPPORT,
            LIBFPTR_ERROR_SHIFT_NUMBERS_DID_NOT_MATCH,
            LIBFPTR_ERROR_DEVICE_NOT_FOUND,
            LIBFPTR_ERROR_EXTERNAL_DEVICE_CONNECTION,
            LIBFPTR_ERROR_DISPENSER_INVALID_STATE,
            LIBFPTR_ERROR_INVALID_POSITIONS_COUNT,
            LIBFPTR_ERROR_DISPENSER_INVALID_NUMBER,
            LIBFPTR_ERROR_INVALID_DIVIDER,
            LIBFPTR_ERROR_FN_ACTIVATION_DENIED,
            LIBFPTR_ERROR_PRINTER_OVERHEAT,
            LIBFPTR_ERROR_FN_EXCHANGE,
            LIBFPTR_ERROR_FN_INVALID_FORMAT,
            LIBFPTR_ERROR_FN_INVALID_STATE,
            LIBFPTR_ERROR_FN_FAULT,
            LIBFPTR_ERROR_FN_CRYPTO_FAULT,
            LIBFPTR_ERROR_FN_EXPIRED,
            LIBFPTR_ERROR_FN_OVERFLOW,
            LIBFPTR_ERROR_FN_INVALID_DATE_TIME,
            LIBFPTR_ERROR_FN_NO_MORE_DATA,
            LIBFPTR_ERROR_FN_TOTAL_OVERFLOW,
            LIBFPTR_ERROR_BUFFER_OVERFLOW,
            LIBFPTR_ERROR_PRINT_SECOND_COPY_DENIED,
            LIBFPTR_ERROR_NEED_RESET_JOURNAL,
            LIBFPTR_ERROR_TAX_SUM_TOO_MUCH,
            LIBFPTR_ERROR_TAX_ON_LAST_OPERATION_DENIED,
            LIBFPTR_ERROR_INVALID_FN_NUMBER,
            LIBFPTR_ERROR_TAX_CANCEL_DENIED,
            LIBFPTR_ERROR_LOW_BATTERY,
            LIBFPTR_ERROR_FN_INVALID_COMMAND,
            LIBFPTR_ERROR_FN_COMMAND_OVERFLOW,
            LIBFPTR_ERROR_FN_NO_TRANSPORT_CONNECTION,
            LIBFPTR_ERROR_FN_CRYPTO_HAS_EXPIRED,
            LIBFPTR_ERROR_FN_RESOURCE_HAS_EXPIRED,
            LIBFPTR_ERROR_INVALID_MESSAGE_FROM_OFD,
            LIBFPTR_ERROR_FN_HAS_NOT_SEND_DOCUMENTS,
            LIBFPTR_ERROR_FN_TIMEOUT,
            LIBFPTR_ERROR_FN_SHIFT_EXPIRED,
            LIBFPTR_ERROR_FN_INVALID_TIME_DIFFERENCE,
            LIBFPTR_ERROR_INVALID_TAXATION_TYPE,
            LIBFPTR_ERROR_INVALID_TAX_TYPE,
            LIBFPTR_ERROR_INVALID_COMMODITY_PAYMENT_TYPE,
            LIBFPTR_ERROR_INVALID_COMMODITY_CODE_TYPE,
            LIBFPTR_ERROR_EXCISABLE_COMMODITY_DENIED,
            LIBFPTR_ERROR_FISCAL_PROPERTY_WRITE,
            LIBFPTR_ERROR_INVALID_COUNTER_TYPE,
            LIBFPTR_ERROR_CUTTER_FAULT,
            LIBFPTR_ERROR_REPORT_INTERRUPTED,
            LIBFPTR_ERROR_INVALID_LEFT_MARGIN,
            LIBFPTR_ERROR_INVALID_ALIGNMENT,
            LIBFPTR_ERROR_INVALID_TAX_MODE,
            LIBFPTR_ERROR_FILE_NOT_FOUND,
            LIBFPTR_ERROR_PICTURE_TOO_BIG,
            LIBFPTR_ERROR_INVALID_BARCODE_PARAMS,
            LIBFPTR_ERROR_FISCAL_PROPERTY_DENIED,
            LIBFPTR_ERROR_FN_INTERFACE,
            LIBFPTR_ERROR_DATA_DUPLICATE,
            LIBFPTR_ERROR_NO_REQUIRED_FISCAL_PROPERTY,
            LIBFPTR_ERROR_FN_READ_DOCUMENT,
            LIBFPTR_ERROR_FLOAT_OVERFLOW,
            LIBFPTR_ERROR_INVALID_SETTING_VALUE,
            LIBFPTR_ERROR_HARD_FAULT,
            LIBFPTR_ERROR_FN_NOT_FOUND,
            LIBFPTR_ERROR_INVALID_AGENT_FISCAL_PROPERTY,
            LIBFPTR_ERROR_INVALID_FISCAL_PROPERTY_VALUE_1002_1056,
            LIBFPTR_ERROR_INVALID_FISCAL_PROPERTY_VALUE_1002_1017,
            LIBFPTR_ERROR_SCRIPT,
            LIBFPTR_ERROR_INVALID_USER_MEMORY_INDEX,
            LIBFPTR_ERROR_NO_ACTIVE_OPERATOR,
            LIBFPTR_ERROR_REGISTRATION_REPORT_INTERRUPTED,
            LIBFPTR_ERROR_CLOSE_FN_REPORT_INTERRUPTED,
            LIBFPTR_ERROR_OPEN_SHIFT_REPORT_INTERRUPTED,
            LIBFPTR_ERROR_OFD_EXCHANGE_REPORT_INTERRUPTED,
            LIBFPTR_ERROR_CLOSE_RECEIPT_INTERRUPTED,
            LIBFPTR_ERROR_FN_QUERY_INTERRUPTED,
            LIBFPTR_ERROR_RTC_FAULT,
            LIBFPTR_ERROR_MEMORY_FAULT,
            LIBFPTR_ERROR_CHIP_FAULT,
            LIBFPTR_ERROR_TEMPLATES_CORRUPTED,
            LIBFPTR_ERROR_INVALID_MAC_ADDRESS,
            LIBFPTR_ERROR_INVALID_SCRIPT_NUMBER,
            LIBFPTR_ERROR_SCRIPTS_FAULT,
            LIBFPTR_ERROR_INVALID_SCRIPTS_VERSION,
            LIBFPTR_ERROR_INVALID_CLICHE_FORMAT,
            LIBFPTR_ERROR_WAIT_FOR_REBOOT,
            LIBFPTR_ERROR_NO_LICENSE,
            LIBFPTR_ERROR_INVALID_FFD_VERSION,
            LIBFPTR_ERROR_CHANGE_SETTING_DENIED,
            LIBFPTR_ERROR_INVALID_NOMENCLATURE_TYPE,
            LIBFPTR_ERROR_INVALID_GTIN,
            LIBFPTR_ERROR_NEGATIVE_MATH_RESULT,
            LIBFPTR_ERROR_FISCAL_PROPERTIES_COMBINATION,
            LIBFPTR_ERROR_OPERATOR_LOGIN,
            LIBFPTR_ERROR_INVALID_INTERNET_CHANNEL,

            LIBFPTR_ERROR_BASE_WEB = 500,
            LIBFPTR_ERROR_RECEIPT_PARSE_ERROR,
            LIBFPTR_ERROR_INTERRUPTED_BY_PREVIOUS_ERRORS,
        };

        enum param
        {
            LIBFPTR_PARAM_FIRST = 65536,
            LIBFPTR_PARAM_TEXT = LIBFPTR_PARAM_FIRST,
            LIBFPTR_PARAM_TEXT_WRAP,
            LIBFPTR_PARAM_ALIGNMENT,

            LIBFPTR_PARAM_FONT,
            LIBFPTR_PARAM_FONT_DOUBLE_WIDTH,
            LIBFPTR_PARAM_FONT_DOUBLE_HEIGHT,
            LIBFPTR_PARAM_LINESPACING,
            LIBFPTR_PARAM_BRIGHTNESS,

            LIBFPTR_PARAM_MODEL,
            LIBFPTR_PARAM_RECEIPT_TYPE,
            LIBFPTR_PARAM_REPORT_TYPE,
            LIBFPTR_PARAM_MODE,
            LIBFPTR_PARAM_EXTERNAL_DEVICE_TYPE,
            LIBFPTR_PARAM_EXTERNAL_DEVICE_DATA,
            LIBFPTR_PARAM_FREQUENCY,
            LIBFPTR_PARAM_DURATION,
            LIBFPTR_PARAM_CUT_TYPE,
            LIBFPTR_PARAM_DRAWER_ON_TIMEOUT,
            LIBFPTR_PARAM_DRAWER_OFF_TIMEOUT,
            LIBFPTR_PARAM_DRAWER_ON_QUANTITY,
            LIBFPTR_PARAM_TIMEOUT_ENQ,
            LIBFPTR_PARAM_COMMAND_BUFFER,
            LIBFPTR_PARAM_ANSWER_BUFFER,
            LIBFPTR_PARAM_SERIAL_NUMBER,
            LIBFPTR_PARAM_MANUFACTURER_CODE,
            LIBFPTR_PARAM_NO_NEED_ANSWER,
            LIBFPTR_PARAM_INFO_DISCOUNT_SUM,
            LIBFPTR_PARAM_USE_ONLY_TAX_TYPE,
            LIBFPTR_PARAM_PAYMENT_TYPE,
            LIBFPTR_PARAM_PAYMENT_SUM,
            LIBFPTR_PARAM_REMAINDER,
            LIBFPTR_PARAM_CHANGE,
            LIBFPTR_PARAM_DEPARTMENT,
            LIBFPTR_PARAM_TAX_TYPE,
            LIBFPTR_PARAM_TAX_SUM,
            LIBFPTR_PARAM_TAX_MODE,
            LIBFPTR_PARAM_RECEIPT_ELECTRONICALLY,
            LIBFPTR_PARAM_USER_PASSWORD,
            LIBFPTR_PARAM_SCALE,
            LIBFPTR_PARAM_LEFT_MARGIN,
            LIBFPTR_PARAM_BARCODE,
            LIBFPTR_PARAM_BARCODE_TYPE,
            LIBFPTR_PARAM_BARCODE_PRINT_TEXT,
            LIBFPTR_PARAM_BARCODE_VERSION,
            LIBFPTR_PARAM_BARCODE_CORRECTION,
            LIBFPTR_PARAM_BARCODE_COLUMNS,
            LIBFPTR_PARAM_BARCODE_INVERT,
            LIBFPTR_PARAM_HEIGHT,
            LIBFPTR_PARAM_WIDTH,
            LIBFPTR_PARAM_FILENAME,
            LIBFPTR_PARAM_PICTURE_NUMBER,
            LIBFPTR_PARAM_DATA_TYPE,
            LIBFPTR_PARAM_OPERATOR_ID,
            LIBFPTR_PARAM_LOGICAL_NUMBER,
            LIBFPTR_PARAM_DATE_TIME,
            LIBFPTR_PARAM_FISCAL,
            LIBFPTR_PARAM_SHIFT_STATE,
            LIBFPTR_PARAM_CASHDRAWER_OPENED,
            LIBFPTR_PARAM_RECEIPT_PAPER_PRESENT,
            LIBFPTR_PARAM_COVER_OPENED,
            LIBFPTR_PARAM_SUBMODE,
            LIBFPTR_PARAM_RECEIPT_NUMBER,
            LIBFPTR_PARAM_DOCUMENT_NUMBER,
            LIBFPTR_PARAM_SHIFT_NUMBER,
            LIBFPTR_PARAM_RECEIPT_SUM,
            LIBFPTR_PARAM_RECEIPT_LINE_LENGTH,
            LIBFPTR_PARAM_RECEIPT_LINE_LENGTH_PIX,
            LIBFPTR_PARAM_MODEL_NAME,
            LIBFPTR_PARAM_UNIT_VERSION,
            LIBFPTR_PARAM_PRINTER_CONNECTION_LOST,
            LIBFPTR_PARAM_PRINTER_ERROR,
            LIBFPTR_PARAM_CUT_ERROR,
            LIBFPTR_PARAM_PRINTER_OVERHEAT,
            LIBFPTR_PARAM_UNIT_TYPE,
            LIBFPTR_PARAM_LICENSE_NUMBER,
            LIBFPTR_PARAM_LICENSE_ENTERED,
            LIBFPTR_PARAM_LICENSE,
            LIBFPTR_PARAM_SUM,
            LIBFPTR_PARAM_COUNT,
            LIBFPTR_PARAM_COUNTER_TYPE,
            LIBFPTR_PARAM_STEP_COUNTER_TYPE,
            LIBFPTR_PARAM_ERROR_TAG_NUMBER,
            LIBFPTR_PARAM_TABLE,
            LIBFPTR_PARAM_ROW,
            LIBFPTR_PARAM_FIELD,
            LIBFPTR_PARAM_FIELD_VALUE,
            LIBFPTR_PARAM_FN_DATA_TYPE,
            LIBFPTR_PARAM_TAG_NUMBER,
            LIBFPTR_PARAM_TAG_VALUE,
            LIBFPTR_PARAM_DOCUMENTS_COUNT,
            LIBFPTR_PARAM_FISCAL_SIGN,
            LIBFPTR_PARAM_DEVICE_FFD_VERSION,
            LIBFPTR_PARAM_FN_FFD_VERSION,
            LIBFPTR_PARAM_FFD_VERSION,
            LIBFPTR_PARAM_CHECK_SUM,
            LIBFPTR_PARAM_COMMODITY_NAME,
            LIBFPTR_PARAM_PRICE,
            LIBFPTR_PARAM_QUANTITY,
            LIBFPTR_PARAM_POSITION_SUM,
            LIBFPTR_PARAM_FN_TYPE,
            LIBFPTR_PARAM_FN_VERSION,
            LIBFPTR_PARAM_REGISTRATIONS_REMAIN,
            LIBFPTR_PARAM_REGISTRATIONS_COUNT,
            LIBFPTR_PARAM_NO_ERROR_IF_NOT_SUPPORTED,
            LIBFPTR_PARAM_OFD_EXCHANGE_STATUS,
            LIBFPTR_PARAM_FN_ERROR_DATA,
            LIBFPTR_PARAM_FN_ERROR_CODE,
            LIBFPTR_PARAM_ENVD_MODE,
            LIBFPTR_PARAM_DOCUMENT_CLOSED,
            LIBFPTR_PARAM_JSON_DATA,
            LIBFPTR_PARAM_COMMAND_SUBSYSTEM,
            LIBFPTR_PARAM_FN_OPERATION_TYPE,
            LIBFPTR_PARAM_FN_STATE,
            LIBFPTR_PARAM_ENVD_MODE_ENABLED,
            LIBFPTR_PARAM_SETTING_ID,
            LIBFPTR_PARAM_SETTING_VALUE,
            LIBFPTR_PARAM_MAPPING_KEY,
            LIBFPTR_PARAM_MAPPING_VALUE,
            LIBFPTR_PARAM_COMMODITY_PIECE,
            LIBFPTR_PARAM_POWER_SOURCE_TYPE,
            LIBFPTR_PARAM_BATTERY_CHARGE,
            LIBFPTR_PARAM_VOLTAGE,
            LIBFPTR_PARAM_USE_BATTERY,
            LIBFPTR_PARAM_BATTERY_CHARGING,
            LIBFPTR_PARAM_CAN_PRINT_WHILE_ON_BATTERY,
            LIBFPTR_PARAM_MAC_ADDRESS,
            LIBFPTR_PARAM_FN_FISCAL,
            LIBFPTR_PARAM_NETWORK_ERROR,
            LIBFPTR_PARAM_OFD_ERROR,
            LIBFPTR_PARAM_FN_ERROR,
            LIBFPTR_PARAM_COMMAND_CODE,
            LIBFPTR_PARAM_PRINTER_TEMPERATURE,
            LIBFPTR_PARAM_RECORDS_TYPE,
            LIBFPTR_PARAM_OFD_FISCAL_SIGN,
            LIBFPTR_PARAM_HAS_OFD_TICKET,
            LIBFPTR_PARAM_NO_SERIAL_NUMBER,
            LIBFPTR_PARAM_RTC_FAULT,
            LIBFPTR_PARAM_SETTINGS_FAULT,
            LIBFPTR_PARAM_COUNTERS_FAULT,
            LIBFPTR_PARAM_USER_MEMORY_FAULT,
            LIBFPTR_PARAM_SERVICE_COUNTERS_FAULT,
            LIBFPTR_PARAM_ATTRIBUTES_FAULT,
            LIBFPTR_PARAM_FN_FAULT,
            LIBFPTR_PARAM_INVALID_FN,
            LIBFPTR_PARAM_HARD_FAULT,
            LIBFPTR_PARAM_MEMORY_MANAGER_FAULT,
            LIBFPTR_PARAM_SCRIPTS_FAULT,
            LIBFPTR_PARAM_FULL_RESET,
            LIBFPTR_PARAM_WAIT_FOR_REBOOT,
            LIBFPTR_PARAM_SCALE_PERCENT,
            LIBFPTR_PARAM_FN_NEED_REPLACEMENT,
            LIBFPTR_PARAM_FN_RESOURCE_EXHAUSTED,
            LIBFPTR_PARAM_FN_MEMORY_OVERFLOW,
            LIBFPTR_PARAM_FN_OFD_TIMEOUT,
            LIBFPTR_PARAM_FN_CRITICAL_ERROR,
            LIBFPTR_PARAM_OFD_MESSAGE_READ,
            LIBFPTR_PARAM_DEVICE_MIN_FFD_VERSION,
            LIBFPTR_PARAM_DEVICE_MAX_FFD_VERSION,
            LIBFPTR_PARAM_DEVICE_UPTIME,
            LIBFPTR_PARAM_NOMENCLATURE_TYPE,
            LIBFPTR_PARAM_GTIN,
            LIBFPTR_PARAM_FN_DOCUMENT_TYPE,
            LIBFPTR_PARAM_NETWORK_ERROR_TEXT,
            LIBFPTR_PARAM_FN_ERROR_TEXT,
            LIBFPTR_PARAM_OFD_ERROR_TEXT,
            LIBFPTR_PARAM_USER_SCRIPT_ID,
            LIBFPTR_PARAM_USER_SCRIPT_PARAMETER,
            LIBFPTR_PARAM_USER_MEMORY_OPERATION,
            LIBFPTR_PARAM_USER_MEMORY_DATA,
            LIBFPTR_PARAM_USER_MEMORY_STRING,
            LIBFPTR_PARAM_USER_MEMORY_ADDRESS,
            LIBFPTR_PARAM_FN_PRESENT,
            LIBFPTR_PARAM_BLOCKED,
            LIBFPTR_PARAM_DOCUMENT_PRINTED,
            LIBFPTR_PARAM_DISCOUNT_SUM,
            LIBFPTR_PARAM_SURCHARGE_SUM,
            LIBFPTR_PARAM_LK_USER_CODE,
            LIBFPTR_PARAM_LICENSE_COUNT,

            LIBFPTR_PARAM_LAST
        };

        enum model
        {
            LIBFPTR_MODEL_UNKNOWN = 0,
            LIBFPTR_MODEL_ATOL_AUTO = 500,
            LIBFPTR_MODEL_ATOL_11F = 67,
            LIBFPTR_MODEL_ATOL_15F = 78,
            LIBFPTR_MODEL_ATOL_20F = 81,
            LIBFPTR_MODEL_ATOL_22F = 63,
            LIBFPTR_MODEL_ATOL_25F = 57,
            LIBFPTR_MODEL_ATOL_30F = 61,
            LIBFPTR_MODEL_ATOL_42FS = 77,
            LIBFPTR_MODEL_ATOL_50F = 80,
            LIBFPTR_MODEL_ATOL_52F = 64,
            LIBFPTR_MODEL_ATOL_55F = 62,
            LIBFPTR_MODEL_ATOL_60F = 75,
            LIBFPTR_MODEL_ATOL_77F = 69,
            LIBFPTR_MODEL_ATOL_90F = 72,
            LIBFPTR_MODEL_ATOL_91F = 82,
            LIBFPTR_MODEL_ATOL_92F = 84,
            LIBFPTR_MODEL_ATOL_SIGMA_10 = 86
        };

        private const string LIBFPTR_SETTING_LIBRARY_PATH = "LibraryPath";
        private const string LIBFPTR_SETTING_MODEL = "Mode= ";
        private const string LIBFPTR_SETTING_PORT = "Port";
        private const string LIBFPTR_SETTING_BAUDRATE = "BaudRate";
        private const string LIBFPTR_SETTING_BITS = "Bits";
        private const string LIBFPTR_SETTING_PARITY = "Parity";
        private const string LIBFPTR_SETTING_STOPBITS = "StopBits";
        private const string LIBFPTR_SETTING_IPADDRESS = "IPAddress";
        private const string LIBFPTR_SETTING_IPPORT = "IPPort";
        private const string LIBFPTR_SETTING_MACADDRESS = "MACAddress";
        private const string LIBFPTR_SETTING_COM_FILE = "ComFile";
        private const string LIBFPTR_SETTING_USB_DEVICE_PATH = "UsbDevicePath";
        private const string LIBFPTR_SETTING_BT_AUTOENABLE = "AutoEnableBluetooth";
        private const string LIBFPTR_SETTING_BT_AUTODISABLE = "AutoDisableBluetooth";
        private const string LIBFPTR_SETTING_ACCESS_PASSWORD = "AccessPassword";
        private const string LIBFPTR_SETTING_USER_PASSWORD = "UserPassword";
        private const string LIBFPTR_SETTING_OFD_CHANNEL = "OfdChanne= ";
        private const string LIBFPTR_SETTING_EXISTED_COM_FILES = "ExistedComFiles";

        enum port
        {
            LIBFPTR_PORT_COM = 0,
            LIBFPTR_PORT_USB,
            LIBFPTR_PORT_TCPIP,
            LIBFPTR_PORT_BLUETOOTH,
        };

        enum baudrate
        {
            LIBFPTR_PORT_BR_1200 = 1200,
            LIBFPTR_PORT_BR_2400 = 2400,
            LIBFPTR_PORT_BR_4800 = 4800,
            LIBFPTR_PORT_BR_9600 = 9600,
            LIBFPTR_PORT_BR_19200 = 19200,
            LIBFPTR_PORT_BR_38400 = 38400,
            LIBFPTR_PORT_BR_57600 = 57600,
            LIBFPTR_PORT_BR_115200 = 115200,
            LIBFPTR_PORT_BR_230400 = 230400,
            LIBFPTR_PORT_BR_460800 = 460800,
            LIBFPTR_PORT_BR_921600 = 921600,
        };

        enum bits
        {
            LIBFPTR_PORT_BITS_7 = 7,
            LIBFPTR_PORT_BITS_8 = 8,
        };

        enum parity
        {
            LIBFPTR_PORT_PARITY_NO = 0,
            LIBFPTR_PORT_PARITY_ODD,
            LIBFPTR_PORT_PARITY_EVEN,
            LIBFPTR_PORT_PARITY_MARK,
            LIBFPTR_PORT_PARITY_SPACE,
        };

        enum stopbits
        {
            LIBFPTR_PORT_SB_1 = 0,
            LIBFPTR_PORT_SB_1_5,
            LIBFPTR_PORT_SB_2
        };

        enum barcode_type
        {
            LIBFPTR_BT_EAN_8 = 0,
            LIBFPTR_BT_EAN_13,
            LIBFPTR_BT_UPC_A,
            LIBFPTR_BT_UPC_E,
            LIBFPTR_BT_CODE_39,
            LIBFPTR_BT_CODE_93,
            LIBFPTR_BT_CODE_128,
            LIBFPTR_BT_CODABAR,
            LIBFPTR_BT_ITF,
            LIBFPTR_BT_ITF_14,
            LIBFPTR_BT_GS1_128,
            LIBFPTR_BT_QR,
            LIBFPTR_BT_PDF417,
            LIBFPTR_BT_AZTEC,
        };

        enum barcode_correction
        {
            LIBFPTR_BC_DEFAULT = 0,
            LIBFPTR_BC_0,
            LIBFPTR_BC_1,
            LIBFPTR_BC_2,
            LIBFPTR_BC_3,
            LIBFPTR_BC_4,
            LIBFPTR_BC_5,
            LIBFPTR_BC_6,
            LIBFPTR_BC_7,
            LIBFPTR_BC_8,
        };

        enum tax_mode
        {
            LIBFPTR_TM_POSITION = 0,
            LIBFPTR_TM_UNIT,
        };

        enum step_counter_type
        {
            LIBFPTR_SCT_OVERALL = 0,
            LIBFPTR_SCT_FORWARD,
        };

        enum counter_type
        {
            LIBFPTR_CT_ROLLUP = 0,
            LIBFPTR_CT_RESETTABLE,
        };

        enum shift_state
        {
            LIBFPTR_SS_CLOSED = 0,
            LIBFPTR_SS_OPENED,
            LIBFPTR_SS_EXPIRED,
        };

        enum cut_type
        {
            LIBFPTR_CT_FULL = 0,
            LIBFPTR_CT_PART,
        };

        enum alignment
        {
            LIBFPTR_ALIGNMENT_LEFT = 0,
            LIBFPTR_ALIGNMENT_CENTER,
            LIBFPTR_ALIGNMENT_RIGHT,
        };

        enum text_wrap
        {
            LIBFPTR_TW_NONE = 0,
            LIBFPTR_TW_WORDS,
            LIBFPTR_TW_CHARS,
        };

        enum fn_type
        {
            LIBFPTR_FNT_DEBUG = 0,
            LIBFPTR_FNT_RELEASE,
            LIBFPTR_FNT_UNKNOWN,
        };

        enum fn_state
        {
            LIBFPTR_FNS_INITIAL = 0,
            LIBFPTR_FNS_CONFIGURED = 1,
            LIBFPTR_FNS_FISCAL_MODE = 3,
            LIBFPTR_FNS_POSTFISCAL_MODE = 7,
            LIBFPTR_FNS_ACCESS_ARCHIVE = 15,
        };

        enum receipt_type
        {
            LIBFPTR_RT_CLOSED = 0,
            LIBFPTR_RT_SELL = 1,
            LIBFPTR_RT_SELL_RETURN = 2,
            LIBFPTR_RT_SELL_CORRECTION = 7,
            LIBFPTR_RT_BUY = 4,
            LIBFPTR_RT_BUY_RETURN = 5,
            LIBFPTR_RT_BUY_CORRECTION = 9,
        };

        enum report_type
        {
            LIBFPTR_RT_CLOSE_SHIFT = 0,
            LIBFPTR_RT_X,
            LIBFPTR_RT_LAST_DOCUMENT,
            LIBFPTR_RT_OFD_EXCHANGE_STATUS,
            LIBFPTR_RT_KKT_DEMO,
            LIBFPTR_RT_KKT_INFO,
            LIBFPTR_RT_OFD_TEST,
            LIBFPTR_RT_FN_DOC_BY_NUMBER,
            LIBFPTR_RT_QUANTITY,
            LIBFPTR_RT_DEPARTMENTS,
            LIBFPTR_RT_OPERATORS,
            LIBFPTR_RT_HOURS,
            LIBFPTR_RT_FN_REGISTRATIONS,
            LIBFPTR_RT_FN_SHIFT_TOTAL_COUNTERS,
            LIBFPTR_RT_FN_TOTAL_COUNTERS,
            LIBFPTR_RT_FN_NOT_SENT_DOCUMENTS_COUNTERS,
            LIBFPTR_RT_COMMODITIES_BY_TAXATION_TYPES,
            LIBFPTR_RT_COMMODITIES_BY_DEPARTMENTS,
            LIBFPTR_RT_COMMODITIES_BY_SUMS,
            LIBFPTR_RT_START_SERVICE
        };

        enum payment_type
        {
            LIBFPTR_PT_CASH = 0,
            LIBFPTR_PT_ELECTRONICALLY,
            LIBFPTR_PT_PREPAID,
            LIBFPTR_PT_CREDIT,
            LIBFPTR_PT_OTHER,
            LIBFPTR_PT_6,
            LIBFPTR_PT_7,
            LIBFPTR_PT_8,
            LIBFPTR_PT_9,
            LIBFPTR_PT_10
        };

        enum tax_type
        {
            LIBFPTR_TAX_DEPARTMENT = 0,
            LIBFPTR_TAX_VAT18 = 1,
            LIBFPTR_TAX_VAT10,
            LIBFPTR_TAX_VAT118,
            LIBFPTR_TAX_VAT110,
            LIBFPTR_TAX_VAT0,
            LIBFPTR_TAX_NO,
        };

        enum external_device_type
        {
            LIBFPTR_EXTERNAL_DEVICE_DISPLAY = 0,
            LIBFPTR_EXTERNAL_DEVICE_PINPAD,
            LIBFPTR_EXTERNAL_DEVICE_MODEM,
            LIBFPTR_EXTERNAL_DEVICE_BARCODE_SCANNER,
        };

        enum kkt_data_type
        {
            LIBFPTR_DT_STATUS = 0,
            LIBFPTR_DT_CASH_SUM,
            LIBFPTR_DT_UNIT_VERSION,
            LIBFPTR_DT_PICTURE_INFO,
            LIBFPTR_DT_LICENSE_ACTIVATED,
            LIBFPTR_DT_REGISTRATIONS_SUM,
            LIBFPTR_DT_REGISTRATIONS_COUNT,
            LIBFPTR_DT_PAYMENT_SUM,
            LIBFPTR_DT_CASHIN_SUM,
            LIBFPTR_DT_CASHIN_COUNT,
            LIBFPTR_DT_CASHOUT_SUM,
            LIBFPTR_DT_CASHOUT_COUNT,
            LIBFPTR_DT_REVENUE,
            LIBFPTR_DT_DATE_TIME,
            LIBFPTR_DT_SHIFT_STATE,
            LIBFPTR_DT_RECEIPT_STATE,
            LIBFPTR_DT_SERIAL_NUMBER,
            LIBFPTR_DT_MODEL_INFO,
            LIBFPTR_DT_RECEIPT_LINE_LENGTH,
            LIBFPTR_DT_CUTTER_RESOURCE,
            LIBFPTR_DT_STEP_RESOURCE,
            LIBFPTR_DT_TERMAL_RESOURCE,
            LIBFPTR_DT_ENVD_MODE,
            LIBFPTR_DT_SHIFT_TAX_SUM,
            LIBFPTR_DT_RECEIPT_TAX_SUM,
            LIBFPTR_DT_NON_NULLABLE_SUM,
            LIBFPTR_DT_RECEIPT_COUNT,
            LIBFPTR_DT_CANCELLATION_COUNT_ALL,
            LIBFPTR_DT_CANCELLATION_SUM,
            LIBFPTR_DT_CANCELLATION_SUM_ALL,
            LIBFPTR_DT_POWER_SOURCE_STATE,
            LIBFPTR_DT_CANCELLATION_COUNT,
            LIBFPTR_DT_NON_NULLABLE_SUM_BY_PAYMENTS,
            LIBFPTR_DT_PRINTER_TEMPERATURE,
            LIBFPTR_DT_FATAL_STATUS,
            LIBFPTR_DT_MAC_ADDRESS,
            LIBFPTR_DT_DEVICE_UPTIME,
            LIBFPTR_DT_RECEIPT_BYTE_COUNT,
            LIBFPTR_DT_DISCOUNT_AND_SURCHARGE_SUM,
            LIBFPTR_DT_LK_USER_CODE,
            LIBFPTR_DT_LAST_SENT_OFD_DOCUMENT_DATE_TIME
        };

        enum fn_data_type
        {
            LIBFPTR_FNDT_TAG_VALUE,
            LIBFPTR_FNDT_OFD_EXCHANGE_STATUS,
            LIBFPTR_FNDT_FN_INFO,
            LIBFPTR_FNDT_LAST_REGISTRATION,
            LIBFPTR_FNDT_LAST_RECEIPT,
            LIBFPTR_FNDT_LAST_DOCUMENT,
            LIBFPTR_FNDT_SHIFT,
            LIBFPTR_FNDT_FFD_VERSIONS,
            LIBFPTR_FNDT_VALIDITY,
            LIBFPTR_FNDT_REG_INFO,
            LIBFPTR_FNDT_DOCUMENTS_COUNT_IN_SHIFT,
            LIBFPTR_FNDT_ERRORS,
            LIBFPTR_FNDT_TICKET_BY_DOC_NUMBER,
            LIBFPTR_FNDT_DOCUMENT_BY_NUMBER
        };

        enum ffd_version
        {
            LIBFPTR_FFD_UNKNOWN = 0,
            LIBFPTR_FFD_1_0 = 100,
            LIBFPTR_FFD_1_0_5 = 105,
            LIBFPTR_FFD_1_1 = 110,
        };

        enum taxation_type
        {
            LIBFPTR_TT_DEFAULT = 0x00,
            LIBFPTR_TT_OSN = 0x01,
            LIBFPTR_TT_USN_INCOME = 0x02,
            LIBFPTR_TT_USN_INCOME_OUTCOME = 0x04,
            LIBFPTR_TT_ENVD = 0x08,
            LIBFPTR_TT_ESN = 0x10,
            LIBFPTR_TT_PATENT = 0x20,
        };

        enum unit_type
        {
            LIBFPTR_UT_FIRMWARE,
            LIBFPTR_UT_CONFIGURATION,
            LIBFPTR_UT_TEMPLATES,
            LIBFPTR_UT_CONTROL_UNIT,
            LIBFPTR_UT_BOOT,
        };

        enum fn_operation_type
        {
            LIBFPTR_FNOP_REGISTRATION = 0,
            LIBFPTR_FNOP_CHANGE_FN,
            LIBFPTR_FNOP_CHANGE_PARAMETERS,
            LIBFPTR_FNOP_CLOSE_ARCHIVE,
        };

        enum agent_type
        {
            LIBFPTR_AT_NONE = 0x00,
            LIBFPTR_AT_BANK_PAYING_AGENT = 0x01,
            LIBFPTR_AT_BANK_PAYING_SUBAGENT = 0x02,
            LIBFPTR_AT_PAYING_AGENT = 0x04,
            LIBFPTR_AT_PAYING_SUBAGENT = 0x08,
            LIBFPTR_AT_ATTORNEY = 0x10,
            LIBFPTR_AT_COMMISSION_AGENT = 0x20,
            LIBFPTR_AT_ANOTHER = 0x40,
        };

        enum ofd_channel
        {
            LIBFPTR_OFD_CHANNEL_NONE = 0,
            LIBFPTR_OFD_CHANNEL_USB,
            LIBFPTR_OFD_CHANNEL_PROTO
        };

        enum power_source_type
        {
            LIBFPTR_PST_POWER_SUPPLY = 0,
            LIBFPTR_PST_RTC_BATTERY,
            LIBFPTR_PST_BATTERY
        };

        enum records_type
        {
            LIBFPTR_RT_LAST_DOCUMENT_LINES,
            LIBFPTR_RT_FN_DOCUMENT_TLVS,
            LIBFPTR_RT_EXEC_USER_SCRIPT
        };

        enum nomenclature_type
        {
            LIBFPTR_NT_FURS = 0,
            LIBFPTR_NT_MEDICINES,
            LIBFPTR_NT_TOBACCO,
        };

        enum fn_document_type
        {
            LIBFPTR_FN_DOC_REGISTRATION = 1,
            LIBFPTR_FN_DOC_OPEN_SHIFT = 2,
            LIBFPTR_FN_DOC_RECEIPT = 3,
            LIBFPTR_FN_DOC_BSO = 4,
            LIBFPTR_FN_DOC_CLOSE_SHIFT = 5,
            LIBFPTR_FN_DOC_CLOSE_FN = 6,
            LIBFPTR_FN_DOC_OPERATOR_CONFIRMATION = 7,
            LIBFPTR_FN_DOC_REREGISTRATION = 11,
            LIBFPTR_FN_DOC_EXCHANGE_STATUS = 21,
            LIBFPTR_FN_DOC_CORRECTION = 31,
            LIBFPTR_FN_DOC_BSO_CORRECTION = 41,
        };

        enum log_level
        {
            LIBFPTR_LOG_ERROR = 0,
            LIBFPTR_LOG_WARN,
            LIBFPTR_LOG_INFO,
            LIBFPTR_LOG_DEBUG
        };

        enum user_memory_operation
        {
            LIBFPTR_UMO_GET_SIZE,
            LIBFPTR_UMO_READ_DATA,
            LIBFPTR_UMO_WRITE_DATA,
            LIBFPTR_UMO_READ_STRING,
            LIBFPTR_UMO_WRITE_STRING,
            LIBFPTR_UMO_COMMIT,
        };

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void libfptr_init_library(IntPtr @params);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern string libfptr_get_version_string();

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_create(ref IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_set_settings(IntPtr handle,
            [MarshalAs(UnmanagedType.LPWStr)] string settings);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void libfptr_destroy(ref IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_get_settings(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, int size);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_get_single_setting(IntPtr handle,
            [MarshalAs(UnmanagedType.LPWStr)] string key,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, int size);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void libfptr_set_single_setting(IntPtr handle,
            [MarshalAs(UnmanagedType.LPWStr)] string key,
            [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_apply_single_settings(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_open(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_close(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_is_opened(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_error_code(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_error_description(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, int size);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void libfptr_set_param_bool(IntPtr handle, int param_id,
            int value);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void libfptr_set_param_int(IntPtr handle, int param_id,
            uint value);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void libfptr_set_param_double(IntPtr handle,
            int param_id,
            double value);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void libfptr_set_param_str(IntPtr handle, int param_id,
            [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void libfptr_set_param_datetime(IntPtr handle,
            int param_id,
            int year, int month, int day,
            int hour, int minute,
            int second);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void libfptr_set_param_bytearray(IntPtr handle,
            int param_id, byte[] value, int size);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_get_param_bool(IntPtr handle, int param_id);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint libfptr_get_param_int(IntPtr handle, int param_id);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern double libfptr_get_param_double(IntPtr handle,
            int param_id);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_get_param_str(IntPtr handle, int param_id,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, int size);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void libfptr_get_param_datetime(IntPtr handle,
            int param_id,
            ref int year, ref int month, ref int day,
            ref int hour, ref int minute,
            ref int second);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_get_param_bytearray(IntPtr handle,
            int param_id,
            byte[] value, int size);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_reset_params(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_run_command(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_beep(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_open_drawer(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_cut(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_device_poweroff(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_device_reboot(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_open_shift(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_reset_summary(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_init_device(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_query_data(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_cash_income(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_cash_outcome(IntPtr handle);


        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_open_receipt(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_cancel_receipt(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_close_receipt(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_check_document_closed(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_receipt_total(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_receipt_tax(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_registration(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_payment(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_report(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_print_text(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_print_cliche(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_begin_nonfiscal_document(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_end_nonfiscal_document(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_print_barcode(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_print_picture(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_print_picture_by_number(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_upload_picture_from_file(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_clear_pictures(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_write_device_setting_raw(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_read_device_setting_raw(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_commit_settings(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_init_settings(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_reset_settings(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_write_date_time(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_write_license(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_fn_operation(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_fn_query_data(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_fn_write_attributes(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_external_device_power_on(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_external_device_power_off(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_external_device_write_data(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_external_device_read_data(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_operator_login(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_process_json(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_read_device_setting(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_write_device_setting(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_begin_read_records(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_read_next_record(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_end_read_records(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_user_memory_operation(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_continue_print(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_init_mgm(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_util_form_tlv(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_util_mapping(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_util_form_nomenclature(IntPtr handle);

        [DllImport("fptr10.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libfptr_log_write([MarshalAs(UnmanagedType.LPWStr)] string tag, int level,
            [MarshalAs(UnmanagedType.LPWStr)] string message);

    }
}
