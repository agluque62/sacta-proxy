/** */
angular.module("sacta_proxy")
    .controller("SprxGlobalCtrl", function ($scope, $interval, $location, $translate, $serv, $lserv) {
        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = 0;

        ctrl.HashCode = 0;
        ctrl.timer = 0;
        ctrl.title = "";

        ctrl.user = "agl1";
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

    /** Funciones  */
        function get_status() {
            $serv.status().then(
                (response) => {
                    if (response.status == 200) {
                        if ((typeof response) == 'object') {
                            $lserv.GlobalStd(response.data);
                        }
                        else {
                            // Seguramente ha vencido la sesion.
                        }
                    }
                    else {
                        // Error en el servidor.
                    }
                },
                (error) => {
                    // Error en el tratamiento de la peticion.
                }
            );
        }

        function getInci() {
            $serv.inci_get().then(function (response) {
                if (response.status == 200 && (typeof response) == 'object') {
                    if (ctrl.HashCode != response.data.hash) {
                        ctrl.listainci = response.data.li;
                        ctrl.HashCode = response.data.hash;
                        inciPaginate();
                    }
                    // console.log(ctrl.listainci);
                    /** */
                    if (userLang != response.data.lang) {
                        userLang = response.data.lang;
                        if (userLang.indexOf("en") == 0)
                            $translate.use('en_US');
                        else if (userLang.indexOf("fr") == 0)
                            $translate.use('fr_FR');
                        else
                            $translate.use('es_ES');
                    }
                }
                else {
                    /** El servidor me devuelve errores... */
                    // window.open(routeForDisconnect, "_self");
                }
            }
                , function (response) {
                    // Error. No puedo conectarme al servidor.
                    // window.open(routeForDisconnect, "_self");
                });
        }

        function getTitle() {
            return "Nucleo Sacta Proxy";
        }



        /** Funcion Periodica del controlador */
        var timer = $interval(function () {

            ctrl.date = moment().format('ll');
            ctrl.hora = moment().format('LTS');

            ctrl.timer++;

            if ((ctrl.timer % 5) == 0) {
                $serv.alive();
                get_status();
            }

            ctrl.title = getTitle();
        }, 1000);

        /** */
        $scope.$on('$viewContentLoaded', function () {
            /** Alertify */
            alertify.defaults.transition = 'zoom';
            alertify.defaults.glossary = {
                title: $lserv.translate("ULISES V 5000 I. Nodebox"),
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


