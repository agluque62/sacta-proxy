/** */
angular
    .module('sacta_proxy')
    .factory('$lserv', function ($q, $http, $filter) {
        /** Para las Traducciones */
        var $translate = $filter('translate');
        var globalStd = {};
        var MainMenuCurrent = 0;

        return {
            translate: function (key) {
                return $translate(key);
            }
            , validate: function (tipo, data, max, min) {
                switch (tipo) {
                    case 0:
                        return true;
                    case 1:                     // IP
                        return ip_val(data);
                    case 2:                     // Numerico entre margenes min <= val <= max
                        return (data >= min && data <= max);
                    case 3:                     // Identificador de Frecuencia VHF
                        return vfrec_val(data);
                    case 4:                     // Identificador de Frecuencia UHF
                        return ufrec_val(data);
                    case 5:
                        return xml_val(data);
                    default:
                        return true;
                }
            }
            , GlobalStd: (std) => {
                if (std) {
                    globalStd = std;
                }
                return globalStd;
            }
            , MenuOption: (opt) => {
                if (opt != undefined) {
                    MainMenuCurrent = opt;
                }
                return MainMenuCurrent;
            }
        };

        //** */
        function ip_val(value) {
            if (value != "" && value.match(regx_ipval) == null)
                return false;
            return true;
        }

        //** XXX.YZ */
        function vfrec_val(value) {
            return value.match(regx_fid_vhf) != null;
        }

        //** XXX.YZ */
        function ufrec_val(value) {
            return value.match(regx_fid_uhf) != null;
        }

        //** Validate XML */
        function xml_val(txt) {
            var xmlDoc;
            // code for IE
            if (window.ActiveXObject) {
                xmlDoc = new ActiveXObject("Microsoft.XMLDOM");
                xmlDoc.async = "false";
                xmlDoc.loadXML(txt);

                if (xmlDoc.parseError.errorCode != 0) {
                    txt = "Error Code: " + xmlDoc.parseError.errorCode + "\n";
                    txt = txt + "Error Reason: " + xmlDoc.parseError.reason;
                    txt = txt + "Error Line: " + xmlDoc.parseError.line;
                    alertify.error(txt);
                    return false;
                }
                else {
                    // alertify.success("No errors found");
                    return true;
                }
            }
            // code for Mozilla, Firefox, Opera, etc.
            else if (document.implementation.createDocument) {
                var parser = new DOMParser();
                var text = txt;
                xmlDoc = parser.parseFromString(text, "text/xml");

                if (xmlDoc.getElementsByTagName("parsererror").length > 0) {

                    //  checkErrorXML(xmlDoc.getElementsByTagName("parsererror")[0]);


                    alertify.error("XML Format Error");
                    return false;
                }
                else {
                    // alertify.success("No errors found");
                    return true;
                }
            }
            else {
                alertify.error('Your browser cannot handle XML validation');
                return false;
            }
        }
    });

