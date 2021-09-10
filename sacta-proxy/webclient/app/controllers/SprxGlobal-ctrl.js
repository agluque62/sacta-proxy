/** */
angular.module("sacta_proxy")
    .controller("SprxGlobalCtrl", function ($scope, $interval, $location, $translate, $serv, $lserv) {
        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = (pag) => {
            if (pag != undefined) {
                $lserv.MenuOption(pag);
            }
            return $lserv.MenuOption();
        };

        ctrl.HashCode = 0;
        ctrl.timer = 0;
        ctrl.title = "";

        ctrl.user = "agl1";
        ctrl.version = "x.x.x";
        ctrl.status = "...";
        ctrl.date = (new Date()).toLocaleDateString();
        ctrl.hora = (new Date()).toLocaleTimeString();
        $location.path("/");

        /** Servicios de la pagina */
        ctrl.decodeHtml = function (html) {
            var txt = document.createElement("textarea");
            txt.innerHTML = html;
            return txt.value;
        };

        ctrl.logs = function () {
            var win = window.open('/logs', '_blank');
            win.focus();
        };

        ctrl.logout = function () {
            $serv.logout();
        }
        ctrl.general_status = (status) => {
            var serv = status.server == 'Simple' ? 'SA' : status.main ? 'DA' : 'DS';
            var scv = status.scv;
            var db = status.db == 'No' ? 'No' : status.dbconn ? 'Con' : 'Des';
            return { txt: 'M: ' + serv + ', S: ' + scv + ', D: ' + db };
        }

    /** Funciones  */
        /**
         * */
        function get_status() {
            console.log("Getting status...");
            $serv.status((status) => {
                $lserv.GlobalStd(status);
                ctrl.user = status.user;
                ctrl.version = status.version;
                ctrl.status = ctrl.general_status(status.global).txt;

                console.log("Status Loaded", status);
            });
        }
        function get_inci() {
            $serv.inci_get((data) => {
                if (ctrl.HashCode != data.hash) {
                    ctrl.listainci = data.li;
                    ctrl.HashCode = data.hash;
                    inciPaginate();
                }
            });
        }
        /**
         * */
        function alive() {
            $serv.alive((data) => {
                ctrl.user=data.user;

            //        if (userLang != response.data.lang) {
            //            userLang = response.data.lang;
            //            if (userLang.indexOf("en") == 0)
            //                $translate.use('en_US');
            //            else if (userLang.indexOf("fr") == 0)
            //                $translate.use('fr_FR');
            //            else
            //                $translate.use('es_ES');
            //        }
            });
        }         
        /**
         * */
        function getTitle() {
            return "Nucleo Sacta Proxy";
        }
        /** 
         *  Funcion Periodica del controlador 
         * */
        var timer = $interval(function () {

            ctrl.date = moment().format('ll');
            ctrl.hora = moment().format('LTS');

            if ((ctrl.timer % 5) == 0) {
                // alive();
                get_status();
            }
            ctrl.timer++;

            ctrl.title = getTitle();
        }, 1000);
        /** 
         */
        $scope.$on('$viewContentLoaded', function () {
            /** Alertify */
            alertify.defaults.transition = 'zoom';
            alertify.defaults.glossary = {
                title: $lserv.translate("Nucleo SACTA PROXY"),
                ok: $lserv.translate("Aceptar"),
                cancel: $lserv.translate("Cancelar")
            };
            get_status();
        });

        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            $interval.cancel(timer);
        });

    });


