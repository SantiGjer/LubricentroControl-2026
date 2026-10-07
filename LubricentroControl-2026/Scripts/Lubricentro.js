/* Lubricentro.js — comportamiento compartido de las pantallas (Site.Master lo carga en todas).

   1. Tablas (table.tabla-abm): orden al hacer clic en un encabezado, filtro por texto y, si la
      tabla lo pide con data-filas-por-pagina, paginado. Todo en el navegador, sobre las filas
      que ya mandó el servidor. Lo que el usuario dejó elegido (filtro, orden, página) sobrevive a
      los postbacks porque se guarda en el campo oculto hdnEstadoTablas del Site.Master: un GET
      nuevo arranca de cero, un postback lo recupera.
        - data-filtro="id": usa ese input como filtro en vez de agregar uno arriba de la tabla.
        - data-sin-filtro: tabla sin filtro (solo orden), para las grillas chicas de un formulario.
        - data-buscar (en una fila): texto extra que el filtro tiene en cuenta aunque no se vea
          (ej. el DNI del cliente en Turnos).
        - Las columnas con clase "sin-orden" en el encabezado (las de Acciones) no se ordenan.
   2. Selectores con búsqueda (.selector-busqueda): campo de texto con la lista de opciones
      desplegable y el botón de búsqueda adentro del mismo campo. Las opciones vienen del
      servidor en data-opciones ([{ "v": valor, "t": texto }]) y el valor elegido queda en el
      HiddenField de adentro. Con data-postback="true" avisa al servidor al elegir (dispara el
      ValueChanged de ese HiddenField).
   3. Modales: Lubricentro.abrirModal(id). El servidor lo llama después de un postback
      (Utilidades/Interfaz.AbrirModal) para que el formulario siga a la vista. */
(function () {
    'use strict';

    var Lubricentro = window.Lubricentro = window.Lubricentro || {};

    var comparador = window.Intl && Intl.Collator
        ? new Intl.Collator('es', { sensitivity: 'base', numeric: true })
        : { compare: function (a, b) { return a < b ? -1 : a > b ? 1 : 0; } };

    // --- Texto -------------------------------------------------------------------------

    // Minúsculas y sin acentos, para que "perez" encuentre "Pérez".
    function normalizar(texto) {
        var t = (texto || '').toString().toLowerCase();
        if (t.normalize) t = t.normalize('NFD').replace(/[̀-ͯ]/g, '');
        return t.replace(/\s+/g, ' ').trim();
    }

    // Solo letras y números: así "20123456786" encuentra "20-12345678-6" y "1143215678"
    // encuentra "11-4321-5678".
    function compactar(texto) {
        return texto.replace(/[^a-z0-9]/g, '');
    }

    function partirEnPalabras(texto) {
        var normalizado = normalizar(texto);
        return normalizado ? normalizado.split(' ') : [];
    }

    // Todas las palabras buscadas tienen que aparecer, en cualquier orden.
    function coincide(palabras, texto, textoCompacto) {
        for (var i = 0; i < palabras.length; i++) {
            var palabra = palabras[i];
            if (texto.indexOf(palabra) >= 0) continue;
            var compacta = compactar(palabra);
            if (compacta && textoCompacto.indexOf(compacta) >= 0) continue;
            return false;
        }
        return true;
    }

    // --- Estado que sobrevive a los postbacks --------------------------------------------

    function campoEstado() {
        return document.getElementById('hdnEstadoTablas');
    }

    // Va codificado (encodeURIComponent): el texto del filtro puede traer "<", y ASP.NET
    // rechaza un postback con un campo que parezca HTML (request validation).
    function leerTodoElEstado() {
        var campo = campoEstado();
        if (!campo || !campo.value) return {};
        try {
            return JSON.parse(decodeURIComponent(campo.value)) || {};
        } catch (e) {
            return {};
        }
    }

    function leerEstado(clave) {
        return leerTodoElEstado()[clave] || null;
    }

    function guardarEstado(clave, valor) {
        var campo = campoEstado();
        if (!campo) return;
        var estado = leerTodoElEstado();
        estado[clave] = valor;
        campo.value = encodeURIComponent(JSON.stringify(estado));
    }

    // --- Valores de una celda para ordenar ----------------------------------------------

    var formatoFecha = /^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:\s+(\d{1,2}):(\d{2}))?$/;

    function leerFecha(texto) {
        var m = formatoFecha.exec(texto);
        if (!m) return null;
        return Date.UTC(+m[3], +m[2] - 1, +m[1], m[4] ? +m[4] : 0, m[5] ? +m[5] : 0);
    }

    // Los importes salen con el formato regional del servidor (N2): el Site.Master publica sus
    // separadores en el <body> para no tener que adivinarlos.
    function leerNumero(texto) {
        var miles = document.body.getAttribute('data-separador-miles') || ',';
        var decimal = document.body.getAttribute('data-separador-decimal') || '.';
        var t = texto.replace(/[$\s ]/g, '');
        if (!t) return null;
        t = t.split(miles).join('');
        if (decimal !== '.') t = t.split(decimal).join('.');
        return /^-?\d+(\.\d+)?$/.test(t) ? parseFloat(t) : null;
    }

    // Decide el tipo de toda la columna mirando sus valores: si todos son fechas se ordena
    // como fecha, si todos son números como número, y si no, como texto.
    function tipoDeColumna(valores) {
        var fechas = true, numeros = true, alguno = false;
        for (var i = 0; i < valores.length; i++) {
            var v = valores[i];
            if (!v) continue;
            alguno = true;
            if (fechas && leerFecha(v) === null) fechas = false;
            if (numeros && leerNumero(v) === null) numeros = false;
            if (!fechas && !numeros) break;
        }
        if (!alguno) return 'texto';
        return fechas ? 'fecha' : numeros ? 'numero' : 'texto';
    }

    // --- Tablas ----------------------------------------------------------------------------

    function textoCelda(fila, columna) {
        var celda = fila.cells[columna];
        return celda ? celda.textContent.replace(/\s+/g, ' ').trim() : '';
    }

    function esOrdenable(th) {
        return !th.classList.contains('sin-orden') && normalizar(th.textContent) !== 'acciones' &&
            normalizar(th.textContent) !== '';
    }

    function crearFiltro(tabla) {
        var barra = document.createElement('div');
        barra.className = 'filtro-tabla';
        var input = document.createElement('input');
        input.type = 'search';
        input.className = 'filtro-tabla-texto';
        input.placeholder = 'Filtrar…';
        input.setAttribute('aria-label', 'Filtrar la tabla');
        barra.appendChild(input);
        tabla.parentNode.insertBefore(barra, tabla);
        return input;
    }

    function iniciarTabla(tabla) {
        tabla.setAttribute('data-tabla-iniciada', '');

        var encabezado = null;
        for (var i = 0; i < tabla.rows.length; i++) {
            var celdas = tabla.rows[i].cells;
            if (celdas.length && celdas[0].tagName === 'TH') {
                encabezado = tabla.rows[i];
                break;
            }
        }
        // Una grilla vacía solo trae la fila con el texto de "no hay datos": nada que ordenar.
        if (!encabezado) return;

        var filas = [];
        for (var j = 0; j < tabla.rows.length; j++) {
            var fila = tabla.rows[j];
            if (fila !== encabezado && fila.cells.length && fila.cells[0].tagName === 'TD')
                filas.push(fila);
        }

        var columnasOrdenables = [];
        for (var c = 0; c < encabezado.cells.length; c++)
            columnasOrdenables.push(esOrdenable(encabezado.cells[c]));

        // Texto de cada fila para el filtro (sin la columna de acciones), calculado una sola vez.
        var datos = filas.map(function (f, indice) {
            var partes = [];
            for (var k = 0; k < f.cells.length; k++)
                if (columnasOrdenables[k]) partes.push(f.cells[k].textContent);
            partes.push(f.getAttribute('data-buscar') || '');
            var texto = normalizar(partes.join(' '));
            return { fila: f, indice: indice, texto: texto, compacto: compactar(texto) };
        });

        var t = {
            tabla: tabla,
            clave: tabla.id || ('tabla' + Array.prototype.indexOf.call(document.querySelectorAll('table.tabla-abm'), tabla)),
            encabezado: encabezado,
            datos: datos,
            columna: -1,
            direccion: 1,
            filtro: '',
            pagina: 0,
            porPagina: parseInt(tabla.getAttribute('data-filas-por-pagina'), 10) || 0,
            tipos: {},
            input: null,
            paginador: null,
            filaVacia: null
        };

        var guardado = leerEstado(t.clave);
        if (guardado) {
            t.columna = typeof guardado.c === 'number' ? guardado.c : -1;
            t.direccion = guardado.d === -1 ? -1 : 1;
            t.filtro = guardado.f || '';
            t.pagina = guardado.p || 0;
        }

        // Encabezados: clic (o Enter) para ordenar; el segundo clic invierte el orden.
        Array.prototype.forEach.call(encabezado.cells, function (th, columna) {
            if (!columnasOrdenables[columna]) return;
            th.classList.add('ordenable');
            th.setAttribute('tabindex', '0');
            th.setAttribute('title', 'Ordenar por esta columna');
            var ordenar = function () {
                if (t.columna === columna) t.direccion = -t.direccion;
                else {
                    t.columna = columna;
                    t.direccion = 1;
                }
                aplicar(t);
            };
            th.addEventListener('click', ordenar);
            th.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    ordenar();
                }
            });
        });

        // Filtro: el input que indique la tabla, o uno nuevo arriba de ella.
        if (!tabla.hasAttribute('data-sin-filtro')) {
            var idFiltro = tabla.getAttribute('data-filtro');
            t.input = idFiltro ? document.getElementById(idFiltro) : crearFiltro(tabla);
        }
        if (t.input) {
            t.input.value = t.filtro;
            t.input.addEventListener('input', function () {
                t.filtro = t.input.value;
                t.pagina = 0;
                aplicar(t);
            });
            // Enter en el filtro no tiene que mandar el formulario (dispararía otro botón).
            t.input.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') e.preventDefault();
            });
        }

        if (t.porPagina > 0) {
            t.paginador = document.createElement('div');
            t.paginador.className = 'paginador-tabla';
            tabla.parentNode.insertBefore(t.paginador, tabla.nextSibling);
        }

        aplicar(t);
    }

    function valoresColumna(t, columna) {
        return t.datos.map(function (d) { return textoCelda(d.fila, columna); });
    }

    function compararFilas(t) {
        var columna = t.columna;
        if (!t.tipos[columna]) t.tipos[columna] = tipoDeColumna(valoresColumna(t, columna));
        var tipo = t.tipos[columna];
        var leer = tipo === 'fecha' ? leerFecha : tipo === 'numero' ? leerNumero : null;

        return function (a, b) {
            var va = textoCelda(a.fila, columna), vb = textoCelda(b.fila, columna);
            var resultado;
            if (leer) {
                var na = va ? leer(va) : null, nb = vb ? leer(vb) : null;
                // Las celdas vacías van siempre al final, en los dos sentidos.
                if (na === null && nb === null) resultado = 0;
                else if (na === null) return 1;
                else if (nb === null) return -1;
                else resultado = na - nb;
            } else {
                if (!va && vb) return 1;
                if (va && !vb) return -1;
                resultado = comparador.compare(va, vb);
            }
            return resultado * t.direccion || a.indice - b.indice;
        };
    }

    function aplicar(t) {
        var palabras = partirEnPalabras(t.filtro);

        var ordenados = t.datos.slice();
        if (t.columna >= 0 && t.columna < t.encabezado.cells.length) ordenados.sort(compararFilas(t));

        var visibles = ordenados.filter(function (d) {
            return !palabras.length || coincide(palabras, d.texto, d.compacto);
        });

        var paginas = t.porPagina > 0 ? Math.max(1, Math.ceil(visibles.length / t.porPagina)) : 1;
        if (t.pagina >= paginas) t.pagina = paginas - 1;
        if (t.pagina < 0) t.pagina = 0;
        var desde = t.porPagina > 0 ? t.pagina * t.porPagina : 0;
        var hasta = t.porPagina > 0 ? desde + t.porPagina : visibles.length;

        var mostrar = {};
        visibles.slice(desde, hasta).forEach(function (d) { mostrar[d.indice] = true; });

        // Reordena las filas en el DOM y alterna el color solo entre las que quedan a la vista.
        var contenedor = t.encabezado.parentNode;
        var alterna = false;
        ordenados.forEach(function (d) {
            contenedor.appendChild(d.fila);
            var visible = !!mostrar[d.indice];
            d.fila.style.display = visible ? '' : 'none';
            d.fila.classList.toggle('fila-alterna', visible && alterna);
            if (visible) alterna = !alterna;
        });

        mostrarFilaVacia(t, visibles.length === 0);

        Array.prototype.forEach.call(t.encabezado.cells, function (th, columna) {
            th.classList.toggle('orden-asc', columna === t.columna && t.direccion === 1);
            th.classList.toggle('orden-desc', columna === t.columna && t.direccion === -1);
            if (th.classList.contains('ordenable'))
                th.setAttribute('aria-sort', columna !== t.columna ? 'none' : t.direccion === 1 ? 'ascending' : 'descending');
        });

        if (t.paginador) dibujarPaginador(t, paginas);

        guardarEstado(t.clave, { f: t.filtro, c: t.columna, d: t.direccion, p: t.pagina });
    }

    function mostrarFilaVacia(t, mostrar) {
        if (!t.filaVacia) {
            t.filaVacia = document.createElement('tr');
            t.filaVacia.className = 'fila-sin-resultados';
            var td = document.createElement('td');
            td.colSpan = t.encabezado.cells.length;
            td.textContent = 'No hay filas que coincidan con el filtro.';
            t.filaVacia.appendChild(td);
        }
        if (mostrar) t.encabezado.parentNode.appendChild(t.filaVacia);
        else if (t.filaVacia.parentNode) t.filaVacia.parentNode.removeChild(t.filaVacia);
    }

    function dibujarPaginador(t, paginas) {
        t.paginador.innerHTML = '';
        t.paginador.hidden = paginas <= 1;
        if (paginas <= 1) return;

        var boton = function (texto, destino, habilitado) {
            var b = document.createElement('button');
            b.type = 'button';
            b.className = 'paginador-boton';
            b.textContent = texto;
            b.disabled = !habilitado;
            b.addEventListener('click', function () {
                t.pagina = destino;
                aplicar(t);
            });
            return b;
        };

        var texto = document.createElement('span');
        texto.className = 'paginador-texto';
        texto.textContent = 'Página ' + (t.pagina + 1) + ' de ' + paginas;

        t.paginador.appendChild(boton('◄ Anterior', t.pagina - 1, t.pagina > 0));
        t.paginador.appendChild(texto);
        t.paginador.appendChild(boton('Siguiente ►', t.pagina + 1, t.pagina < paginas - 1));
    }

    function iniciarTablas(raiz) {
        var tablas = raiz.querySelectorAll('table.tabla-abm');
        for (var i = 0; i < tablas.length; i++)
            if (!tablas[i].hasAttribute('data-tabla-iniciada')) iniciarTabla(tablas[i]);
    }

    // --- Selectores con búsqueda ---------------------------------------------------------

    var MaximoOpcionesVisibles = 100;

    function iniciarSelector(contenedor) {
        contenedor.setAttribute('data-selector-iniciado', '');

        var texto = contenedor.querySelector('.selector-texto');
        var oculto = contenedor.querySelector('input[type="hidden"]');
        var boton = contenedor.querySelector('.selector-boton');
        if (!texto || !oculto) return;

        var opciones;
        try {
            opciones = JSON.parse(contenedor.getAttribute('data-opciones') || '[]');
        } catch (e) {
            opciones = [];
        }
        opciones.forEach(function (o) {
            o.normalizado = normalizar(o.t);
            o.compacto = compactar(o.normalizado);
        });

        var deshabilitado = texto.disabled || texto.readOnly;
        if (boton) boton.disabled = deshabilitado;
        if (deshabilitado) return;

        var lista = document.createElement('ul');
        lista.className = 'selector-lista';
        lista.setAttribute('role', 'listbox');
        lista.hidden = true;
        contenedor.appendChild(lista);
        texto.setAttribute('role', 'combobox');
        texto.setAttribute('aria-autocomplete', 'list');
        texto.setAttribute('aria-expanded', 'false');

        // El campo muestra siempre el texto de la opción elegida: si el usuario escribió algo y
        // mandó el formulario sin elegir, al volver del servidor se corrige acá. Si lo elegido
        // no está entre las opciones (ej. un dueño ya dado de baja), queda el texto del servidor.
        for (var i = 0; i < opciones.length; i++) {
            if (opciones[i].v === oculto.value) {
                texto.value = opciones[i].t;
                break;
            }
        }
        if (!oculto.value) texto.value = '';

        // Si el usuario escribe y se va sin elegir nada, el campo vuelve a mostrar lo elegido.
        var textoElegido = texto.value;
        var visibles = [];
        var activa = -1;

        function dibujar(filtro) {
            var palabras = partirEnPalabras(filtro);
            visibles = opciones.filter(function (o) {
                return !palabras.length || coincide(palabras, o.normalizado, o.compacto);
            });
            lista.innerHTML = '';
            activa = -1;

            if (!visibles.length) {
                var vacio = document.createElement('li');
                vacio.className = 'selector-vacio';
                vacio.textContent = opciones.length ? 'No hay coincidencias.' : 'No hay opciones cargadas.';
                lista.appendChild(vacio);
                return;
            }

            visibles.slice(0, MaximoOpcionesVisibles).forEach(function (o, indice) {
                var li = document.createElement('li');
                li.className = 'selector-opcion' + (o.v === oculto.value ? ' elegida' : '');
                li.setAttribute('role', 'option');
                li.textContent = o.t;
                li.addEventListener('click', function () { elegir(o); });
                li.addEventListener('mousemove', function () { marcar(indice); });
                lista.appendChild(li);
            });

            if (visibles.length > MaximoOpcionesVisibles) {
                var mas = document.createElement('li');
                mas.className = 'selector-vacio';
                mas.textContent = 'Hay más resultados: escribí para acotar la búsqueda.';
                lista.appendChild(mas);
            }
        }

        function marcar(indice) {
            var items = lista.querySelectorAll('.selector-opcion');
            if (!items.length) return;
            if (indice < 0) indice = items.length - 1;
            if (indice >= items.length) indice = 0;
            if (activa >= 0 && items[activa]) items[activa].classList.remove('activa');
            activa = indice;
            items[activa].classList.add('activa');
            items[activa].scrollIntoView({ block: 'nearest' });
        }

        function abrir(filtro) {
            dibujar(filtro);
            lista.hidden = false;
            contenedor.classList.add('abierto');
            texto.setAttribute('aria-expanded', 'true');
        }

        function cerrar() {
            lista.hidden = true;
            contenedor.classList.remove('abierto');
            texto.setAttribute('aria-expanded', 'false');
            activa = -1;
        }

        function elegir(opcion) {
            var cambio = oculto.value !== opcion.v;
            oculto.value = opcion.v;
            texto.value = opcion.t;
            textoElegido = opcion.t;
            cerrar();
            if (cambio && contenedor.getAttribute('data-postback') === 'true') {
                if (typeof window.__doPostBack === 'function') window.__doPostBack(oculto.name, '');
                else if (oculto.form) oculto.form.submit();
            }
        }

        // El texto mostrado coincide con lo elegido: abrir muestra todas las opciones; si el
        // usuario ya escribió algo, muestra solo las que coinciden.
        function filtroActual() {
            return texto.value === textoElegido ? '' : texto.value;
        }

        texto.addEventListener('click', function () {
            if (lista.hidden) abrir(filtroActual());
        });

        texto.addEventListener('input', function () {
            abrir(texto.value);
        });

        texto.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                e.preventDefault();
                if (lista.hidden) abrir(filtroActual());
                marcar(activa + (e.key === 'ArrowDown' ? 1 : -1));
            } else if (e.key === 'Enter') {
                // Nunca manda el formulario: Enter elige la opción marcada (o la única que quedó).
                e.preventDefault();
                if (!lista.hidden && activa >= 0 && visibles[activa]) elegir(visibles[activa]);
                else if (!lista.hidden && visibles.length === 1) elegir(visibles[0]);
            } else if (e.key === 'Escape') {
                if (!lista.hidden) {
                    e.preventDefault();
                    e.stopPropagation();
                    cerrar();
                    texto.value = textoElegido;
                }
            }
        });

        texto.addEventListener('blur', function () {
            setTimeout(function () {
                if (contenedor.contains(document.activeElement)) return;
                cerrar();
                if (texto.value !== textoElegido) texto.value = textoElegido;
            }, 150);
        });

        if (boton) {
            boton.addEventListener('click', function () {
                if (lista.hidden) {
                    texto.focus();
                    abrir(filtroActual());
                } else {
                    cerrar();
                }
            });
        }

        // Que hacer clic en la lista no le saque el foco al campo (si no, se cerraría antes
        // de registrar el clic en la opción).
        lista.addEventListener('mousedown', function (e) { e.preventDefault(); });
    }

    function iniciarSelectores(raiz) {
        var selectores = raiz.querySelectorAll('.selector-busqueda');
        for (var i = 0; i < selectores.length; i++)
            if (!selectores[i].hasAttribute('data-selector-iniciado')) iniciarSelector(selectores[i]);
    }

    // --- Modales -----------------------------------------------------------------------------

    Lubricentro.abrirModal = function (id) {
        var abrir = function () {
            var elemento = document.getElementById(id);
            if (!elemento || !window.bootstrap) return;

            // Después de un postback parcial (UpdatePanel adentro del modal) ya está abierto.
            if (elemento.classList.contains('show')) return;

            // Sin la animación de entrada: después de un postback el formulario tiene que
            // verse como si nunca se hubiera cerrado. Al cerrarlo sí se anima.
            elemento.classList.remove('fade');
            elemento.addEventListener('shown.bs.modal', function () {
                elemento.classList.add('fade');
            }, { once: true });
            bootstrap.Modal.getOrCreateInstance(elemento).show();
        };

        // El servidor lo llama al final del formulario, antes de que cargue bootstrap.bundle.js.
        if (document.readyState === 'loading' || !window.bootstrap)
            document.addEventListener('DOMContentLoaded', abrir);
        else
            abrir();
    };

    // --- Arranque ----------------------------------------------------------------------------

    function iniciar() {
        iniciarTablas(document);
        iniciarSelectores(document);
    }

    // Sys.Application.add_load corre al cargar la página y otra vez después de cada postback
    // parcial de un UpdatePanel (que reemplaza su contenido por elementos nuevos, sin iniciar).
    if (window.Sys && Sys.Application) Sys.Application.add_load(iniciar);
    else document.addEventListener('DOMContentLoaded', iniciar);

    Lubricentro.iniciar = iniciar;
})();
