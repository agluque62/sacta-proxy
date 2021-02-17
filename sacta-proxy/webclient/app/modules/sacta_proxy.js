/** Variables Globales */
var Simulate = location.port == 1444;
var pollingTime = 5000;
var maxPreconf = Simulate == true ? 16 : 8;
var userLang = navigator.language;
var DialogTitle = "Nucleo Sacta Proxy";

/** */
var sacta_proxy = angular.module('sacta_proxy', ['ngRoute', 'ui.bootstrap', 'pascalprecht.translate']);

/** */
sacta_proxy.config(function ($translateProvider) {
    // Our translations will go in here
    $translateProvider.useStaticFilesLoader({
        prefix: '/languages/',
        suffix: '.json'
    });

    if (userLang.indexOf("en") == 0)
        $translateProvider.use('en_US');
    else if (userLang.indexOf("fr") == 0)
        $translateProvider.use('fr_FR');
    else
        $translateProvider.use('es_ES');

    /** Configuraicion de las fechas a traves de momment */
    /** Español */
    moment.locale('es', {
        months: 'enero_febrero_marzo_abril_mayo_junio_julio_agosto_septiempbre_octubre_noviembre_diciembre'.split('_'),
        monthsShort: 'ene._feb._mar._abr._may._jun._jul._ago._sep._oct._nov._dic.'.split('_'),
        monthsParseExact: true,
        weekdays: 'domingo_lunes_martes_miercoles_jueves_viernes_sabado'.split('_'),
        weekdaysShort: 'dom._lun._mar._mie._jue._vie._sab.'.split('_'),
        weekdaysMin: 'Do_Lu_Ma_Mi_Ju_Vi_Sa'.split('_'),
        weekdaysParseExact: true,
        longDateFormat: {
            LT: 'HH:mm',
            LTS: 'HH:mm:ss',
            L: 'DD/MM/YYYY',
            LL: 'D MMMM YYYY',
            LLL: 'D MMMM YYYY HH:mm',
            LLLL: 'dddd D MMMM YYYY HH:mm'
        },
        calendar: {
            sameDay: '[Hoy a las] LT',
            nextDay: '[Mañana a las] LT',
            nextWeek: 'dddd [a las] LT',
            lastDay: '[Ayer a las] LT',
            lastWeek: 'dddd [anterior a las] LT',
            sameElse: 'L'
        },
        relativeTime: {
            future: 'en %s',
            past: 'hace %s',
            s: 'algunos segundos',
            m: 'un minuto',
            mm: '%d minutos',
            h: 'una hora',
            hh: '%d horas',
            d: 'un dia',
            dd: '%d dias',
            M: 'un mes',
            MM: '%d meses',
            y: 'un año',
            yy: '%d años'
        },
        dayOfMonthOrdinalParse: /\d{1,2}(er|e)/,
        ordinal: function (number) {
            return number + (number === 1 ? 'er' : 'e');
        },
        meridiemParse: /PM|AM/,
        isPM: function (input) {
            return input.charAt(0) === 'M';
        },
        // In case the meridiem units are not separated around 12, then implement
        // this function (look at locale/id.js for an example).
        // meridiemHour : function (hour, meridiem) {
        //     return /* 0-23 hour, given meridiem token and hour 1-12 */ ;
        // },
        meridiem: function (hours, minutes, isLower) {
            return hours < 12 ? 'PM' : 'AM';
        },
        week: {
            dow: 1, // Monday is the first day of the week.
            doy: 4  // The week that contains Jan 4th is the first week of the year.
        }
    });
    /** Francés */
    moment.locale('fr', {
        months: 'janvier_février_mars_avril_mai_juin_juillet_août_septembre_octobre_novembre_décembre'.split('_'),
        monthsShort: 'janv._févr._mars_avr._mai_juin_juil._août_sept._oct._nov._déc.'.split('_'),
        monthsParseExact: true,
        weekdays: 'dimanche_lundi_mardi_mercredi_jeudi_vendredi_samedi'.split('_'),
        weekdaysShort: 'dim._lun._mar._mer._jeu._ven._sam.'.split('_'),
        weekdaysMin: 'Di_Lu_Ma_Me_Je_Ve_Sa'.split('_'),
        weekdaysParseExact: true,
        longDateFormat: {
            LT: 'HH:mm',
            LTS: 'HH:mm:ss',
            L: 'DD/MM/YYYY',
            LL: 'D MMMM YYYY',
            LLL: 'D MMMM YYYY HH:mm',
            LLLL: 'dddd D MMMM YYYY HH:mm'
        },
        calendar: {
            sameDay: '[Aujourd’hui à] LT',
            nextDay: '[Demain à] LT',
            nextWeek: 'dddd [à] LT',
            lastDay: '[Hier à] LT',
            lastWeek: 'dddd [dernier à] LT',
            sameElse: 'L'
        },
        relativeTime: {
            future: 'dans %s',
            past: 'il y a %s',
            s: 'quelques secondes',
            m: 'une minute',
            mm: '%d minutes',
            h: 'une heure',
            hh: '%d heures',
            d: 'un jour',
            dd: '%d jours',
            M: 'un mois',
            MM: '%d mois',
            y: 'un an',
            yy: '%d ans'
        },
        dayOfMonthOrdinalParse: /\d{1,2}(er|e)/,
        ordinal: function (number) {
            return number + (number === 1 ? 'er' : 'e');
        },
        meridiemParse: /PD|MD/,
        isPM: function (input) {
            return input.charAt(0) === 'M';
        },
        // In case the meridiem units are not separated around 12, then implement
        // this function (look at locale/id.js for an example).
        // meridiemHour : function (hour, meridiem) {
        //     return /* 0-23 hour, given meridiem token and hour 1-12 */ ;
        // },
        meridiem: function (hours, minutes, isLower) {
            return hours < 12 ? 'PD' : 'MD';
        },
        week: {
            dow: 1, // Monday is the first day of the week.
            doy: 4  // The week that contains Jan 4th is the first week of the year.
        }
    });
    /** Formato de fecha segun el lenguaje del navegador*/
    moment.locale(userLang.indexOf("en") == 0 ? "en" : userLang.indexOf("fr") == 0 ? "fr" : "es");

});
/** Directivas Angular */
sacta_proxy.directive('fileModel', ['$parse', function ($parse) {
    return {
        restrict: 'A',
        link: function (scope, element, attrs) {
            var model = $parse(attrs.fileModel);
            var modelSetter = model.assign;
            element.bind('change', function () {
                scope.$apply(function () {
                    modelSetter(scope, element[0].files[0]);
                });
            });
        }
    };
}]);

sacta_proxy.directive('ignoreDirty', [function () {
    return {
        restrict: 'A',
        require: 'ngModel',
        link: function (scope, elm, attrs, ctrl) {
            ctrl.$setPristine = function () { };
            ctrl.$pristine = false;
        }
    }
}]);


//**  Rutinas genéricas */
function StringCut(str, maxlen) {
    var retorno = str.length > maxlen ? str.substring(0, maxlen) + "..." : str;
    return retorno;
}
// Para desordenar un Array en pruebas...
function shuffle(array) {
    var currentIndex = array.length, temporaryValue, randomIndex;

    // Mientras queden elementos a mezclar...
    while (0 !== currentIndex) {

        // Seleccionar un elemento sin mezclar...
        randomIndex = Math.floor(Math.random() * currentIndex);
        currentIndex -= 1;

        // E intercambiarlo con el elemento actual
        temporaryValue = array[currentIndex];
        array[currentIndex] = array[randomIndex];
        array[randomIndex] = temporaryValue;
    }

    return array;
}

/** Alertify Confirm Sync */
function SyncAlertifyConfirm(msg) {
    return new Promise((resolve) => {
        alertify.confirm(DialogTitle, msg,
            function () {
                resolve(true);
            }
            , function () {
                resolve(false);
            });
    });
}
async function NuConfirm(msg) {
    var res = await SyncAlertifyConfirm(msg);
    return res;
}

/** Rutas de Aplicacion */
var routeDefault = "/";
var routeConfig = "/config";
var routeAbout = "/about";

/** Peticiones REST */
var rest_url_alive = "/alive";
var rest_url_inci = "/inci";
var rest_url_std = "/status";
var rest_url_conf = "/config";
var rest_url_logs = "/logs";
var rest_url_logout = "/logout";
var HistoryData = Simulate == true ? "/simulate/history.json" : "/history";

/** */
var roles = {
    ADMIN_PROFILE: 64,
    ING_PROFILE: 32,
    GEST_POFILE: 16,
    CRTL_PROFILE: 8,
    ALM1_PROFILE: 4,
    ALM2_PROLIFLE: 2,
    VIS_PROFILE: 1
};

var srvtypes = { None: "None", Mixed: "Mixed", Phone: "Phone", Radio: "Radio" };
var states = { Running: "Running", Stopped: "Stopped", Disabled: "Disabled" };
var levels = { Master: "Master", Slave: "Slave", Error: "Error" };
var HistoryCodes = [
    { code: 1, descr: "Arranque Servicio" },
    { code: 2, descr: "Parada Servicio" },
    { code: 3, descr: "Error Grave" },
    { code: 4, descr: "Entrando en Modo" },
    { code: 10, descr: "Login" },
    { code: 11, descr: "Error Login" },
    { code: 12, descr: "Cambio de Configuracion" },
    { code: 13, descr: "Logout" },
    { code: 20, descr: "Estado Dependencia" },
    { code: 21, descr: "Estado Tx Dependencia" },
    { code: 22, descr: "Sectorizacion Recibida" },
    { code: 23, descr: "Sectorizacion Rechazada" },
    { code: 25, descr: "Sectorizacion Enviada" },
];

/** */
var routeForUnauthorizedAccess = '/noaut';

/** Validadores. */
var regx_all = /^.*$/;
var regx_ipval = /^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$/;
var regx_trpval = /^[1-2]+,(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\/[0-9]{2,5}$/;
var regx_atsrango = /^[0-9]{6}-[0-9]{6}$/;
var regx_atsnumber = /^[0-9]{6}$/;
var regx_urlval = /^(http(?:s)?\:\/\/[a-zA-Z0-9]+(?:(?:\.|\-)[a-zA-Z0-9]+)+(?:\:\d+)?(?:\/[\w\-]+)*(?:\/?|\/\w+\.[a-zA-Z]{2,4}(?:\?[\w]+\=[\w\-]+)?)?(?:\&[\w]+\=[\w\-]+)*)$/;
var regx_ipportval = /^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(:[\d]{1,5})?$/;
var regx_urival = /^sip:(.+)@(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(:[\d]{1,5})?$/;
var regx_fid = /^(1|2|3)[0-9]{2}\.[0-9]{2}(0|5)$/;
var regx_fid_vhf = /^(1)(1|2|3)([0-9]{1})\.([0-9])(0|2|5|7)(0|5)$/;   /** 118.000 137.000 */
var regx_fid_uhf = /^(2|3|4)([0-9]{2})\.([0-9])(0|2|5|7)(0|5)$/;      /** 225.000 400.000 */
var regx_int = /^\d+$/;
var regx_list_int = /^\d+(?:[ \t]*,[ \t]*\d+)*$/;
var regx_mkip = /((([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]))\/((\b([1-9]|[12][0-9]|3[0-2])\b))$/; // 

/** Validacion por directivas Angular y expresiones regulares */
function smapTest(value) {
    if (typeof value == "string") {
        if (value == "") return true;
        var ret = true;
        var entries = value.split(',');
        entries.forEach((entry) => {
            if (entry.includes(":")) {
                var pair = entry.split(":");
                if (pair.length == 2) {
                    if (pair[0].match(regx_int) && pair[1].match(regx_int))
                        return;
                }
            } 
            ret = false;
        });
        return ret;
    }
    return false;
}
function regx_get(id, value) {
    if (id == "ip") return regx_ipval;
    if (id == "lnum") return regx_list_int;
    if (id == "mkip") return regx_mkip;
    if (id == "smap") {
        return smapTest(value) == true ? regx_all : undefined;
    }

    return undefined;
}
sacta_proxy.directive('nuValidate', function () {
    return {
        require: 'ngModel',
        link: function (scope, element, attr, mCtrl) {
            function Validation(value) {
                var valid = false;
                var regex = regx_get(attr.nuValidate, value);
                if (regex != undefined && typeof value == "string") {
                    valid = value == "" ? true : value.match(regex) != null;
                }
                console.log('nuValidate', attr.nuValidate, value, valid);
                mCtrl.$setValidity('nuValidate', valid);
                return value;
            }
            mCtrl.$parsers.push(Validation);
        }
    };
});


/** MOCK Backend*/
if (Simulate) {
    /** Mock Backend*/
    sacta_proxy
        .constant('Config', {
            useMocks: Simulate,
            view_dir: 'views/',
            API: {
                protocol: 'http',
                host: 'api.example.com',
                port: '8080',
                path: './simulate',
                //fakeDelay: 2000
                fakeDelay: 10
            }
        })
        .config(function (Config, $provide) {
            //Decorate backend with awesomesauce
            if (Config.useMocks) $provide.decorator('$httpBackend', angular.mock.e2e.$httpBackendDecorator);
        })
        .config(function ($httpProvider, Config) {

            if (!Config.useMocks) return;

            $httpProvider.interceptors.push(function ($q, $timeout, Config, $log) {
                return {
                    'request': function (config) {
                        //$log.log('Requesting ' + config.url, config);
                        return config;
                    },
                    'response': function (response) {
                        var deferred = $q.defer();

                        if (response.config.url.indexOf(Config.view_dir) == 0) return response; //Let through views immideately

                        //Fake delay on response from APIs and other urls
                        //$log.log('Delaying response ' + response + ' with ' + Config.API.fakeDelay + 'ms');
                        $timeout(function () {
                            deferred.resolve(response);
                        }, Config.API.fakeDelay);

                        return deferred.promise;
                    }
                };
            });
        })
        .factory('APIBase', function (Config) {
            var request = Config.API.protocol + '://' + Config.API.host + ':' + Config.API.port + Config.API.path + '/';
            return (request);
        })
        .run(function (Config, $httpBackend, $log, APIBase, $timeout) {
            //Only load mocks if config says so
            if (!Config.useMocks) return;

            var messages = {};
            messages.data = [{ id: 1, text: 'Hello World' }];
            messages.index = {};

            angular.forEach(messages.data, function (item, key) {
                messages.index[item.id] = item; //Index messages to be able to do efficient lookups on id
            });

            //Escape string to be able to use it in a regular expression
            function regEsc(str) {
                return str.replace(/[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g, "\\$&");
            }

            //When backend receives a request to the views folder, pass it through
            // $httpBackend.whenGET(RegExp(regEsc(Config.view_dir))).passThrough();
            $httpBackend.whenGET().passThrough();

            ////Message should return a list og messages
            //$httpBackend.whenGET(APIBase + 'messages').respond(function (method, url, data, headers) {
            //    return [200, messages.data, {/*headers*/ }];            //});

            //$httpBackend.whenPOST(APIBase + 'messages').respond(function (method, url, data, headers) {
            //    var message = angular.fromJson(data);

            //    messages.data.push(message);
            //    //You should consider having the back-end being responsible for creating new id tho!
            //    messages.index[message.id] = message;

            //    return [200, message, {/*headers*/ }];
            //});
            $httpBackend.whenPOST().respond(function (method, url, data, headers) {
                var message = angular.fromJson(data);
                var what = Math.floor(Math.random() * 5); // returns a random integer from 0 to 5
                switch (what) {
                    case 0:
                        return [404, "Equipo no Encontrado.", {/*headers*/ }];
                    case 1:
                        return [500, "Error al ejecutar la operacion.", {/*headers*/ }];
                    default:
                        return [200, "Operacion Ejecutada.", {/*headers*/ }];
                }
            });

            //Message/id should return a message
            //$httpBackend.whenGET(new RegExp(regEsc(APIBase + 'messages/') + '\\d+$')).respond(function (method, url, data, headers) {
            //    var id = url.match(/\d+$/)[0];
            //    return [200, messages.index[id] || null, {/*headers*/ }];
            //});

        });

}
