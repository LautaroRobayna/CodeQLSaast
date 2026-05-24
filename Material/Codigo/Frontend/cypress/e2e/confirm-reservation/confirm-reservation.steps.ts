import { Given, When, Then } from 'cypress-cucumber-preprocessor/steps';

Given('que el sistema tiene la reserva {string} en estado {string}', (codigoReserva: string, estadoInicial: string) => {
  cy.wrap({ codigo: codigoReserva, estado: estadoInicial }).as('reservaSeleccionada');

  cy.intercept('GET', '**/api/reservation/pending', {
    statusCode: 200,
    fixture: 'pending-reservations.json'
  }).as('listaReservasPendientes');
});

Given('la reserva {string} contiene {int} unidades de {string} y {int} unidades de {string}',
  (codigoReserva: string, cantidad1: number, medicamento1: string, cantidad2: number, medicamento2: string) => {
  cy.fixture('pending-reservations').then((reservas: any[]) => {
    const reserva = reservas.find((r: any) => r.code === codigoReserva);
    expect(reserva).to.exist;
    const detalle1 = reserva.details.find((d: any) => d.drugName === medicamento1);
    expect(detalle1).to.exist;
    expect(detalle1.quantity).to.equal(cantidad1);
    const detalle2 = reserva.details.find((d: any) => d.drugName === medicamento2);
    expect(detalle2).to.exist;
    expect(detalle2.quantity).to.equal(cantidad2);
  });
});

Given('el medicamento {string} no requiere receta médica', (medicamento: string) => {
  cy.log(`El medicamento "${medicamento}" no necesita receta — se omite validación`);
});

Given('el medicamento {string} requiere receta médica', (medicamento: string) => {
  cy.wrap(medicamento).as('medicamentoConReceta');
});

Given('el empleado {string} con rol {string} inicia sesión en el sistema', (nombreEmpleado: string, rol: string) => {
  const datosSesion = JSON.stringify({
    userName: nombreEmpleado,
    role: rol === 'Empleado Farmacia' ? 'Employee' : rol,
    token: 'jwt-simulado-para-test'
  });
  localStorage.setItem('login', datosSesion);
});

Given('accede al panel de empleado en {string}', (ruta: string) => {
  cy.visit(`http://localhost:4200${ruta}`);
});

Given('hace clic en {string} para ir a la gestión de reservas', (botonSelector: string) => {
  cy.get(botonSelector).click();
});

Given('se encuentra en la página de validación {string}', (rutaValidacion: string) => {
  cy.visit(`http://localhost:4200${rutaValidacion}`);
  cy.wait('@listaReservasPendientes');
});

Given('selecciona la reserva {string} de la lista de pendientes', (codigoReserva: string) => {
  cy.contains(codigoReserva).click();
});

Given('hace clic en el archivo {string} de la lista de recetas adjuntas', (_nombreArchivo: string) => {
  cy.get('.receta-link').first().click();
});

Given('visualiza la receta de {string} en el visor {string}', (nombreMedicamento: string, selectorVisor: string) => {
  cy.get(selectorVisor).should('be.visible');
});

When('hace clic en {string}', (selector: string) => {
  if (selector === '#btn-rechazar-receta') {
    cy.get('@reservaSeleccionada').then((reserva: any) => {
      const codigo = reserva.codigo;
      cy.intercept('PUT', `**/api/reservation/${codigo}/reject`, {
        statusCode: 200,
        fixture: 'reject-response.json'
      }).as('rechazoExitoso');
    });

    cy.intercept('GET', '**/api/reservation/pending', {
      statusCode: 200,
      body: []
    }).as('pendientesTrasConfirmar');

    cy.get(selector).click();
    cy.wait('@rechazoExitoso');
  }
});

When('confirma la reserva haciendo clic en {string}', (selectorBoton: string) => {
  cy.get('@reservaSeleccionada').then((reserva: any) => {
    const codigo = reserva.codigo;

    cy.intercept('PUT', `**/api/reservation/${codigo}/confirm`, {
      statusCode: 200,
      fixture: 'confirm-response.json'
    }).as('confirmacionExitosa');
  });

  cy.intercept('GET', '**/api/reservation/pending', {
    statusCode: 200,
    body: []
  }).as('pendientesTrasConfirmar');

  cy.get(selectorBoton).click();
  cy.wait('@confirmacionExitosa');
});

Then('el sistema debe mostrar un mensaje modal con el texto {string}', (mensajeEsperado: string) => {
  cy.get('.modal').should('be.visible');
  cy.get('.modal').should('contain.text', mensajeEsperado);
});

Then('el sistema debe cambiar el estado de la reserva a {string}', (estadoEsperado: string) => {
  const alias = estadoEsperado === 'Cancelled' ? '@rechazoExitoso' : '@confirmacionExitosa';
  cy.get(alias).then((intercepcion: any) => {
    expect(intercepcion.response).to.exist;
    expect(intercepcion.response.body.status).to.equal(estadoEsperado);
  });
});

Then('la reserva {string} no debe aparecer en la lista de pendientes', (codigoReserva: string) => {
  cy.wait('@pendientesTrasConfirmar').then(() => {
    cy.contains(codigoReserva).should('not.exist');
  });
});

Given('una reserva pendiente sin receta médica', () => {
  cy.wrap({ codigo: 'RES-NO-RECIPE', estado: 'Pending' }).as('reservaSeleccionada');
  cy.intercept('GET', '**/api/reservation/pending', {
    statusCode: 200,
    fixture: 'reservations-no-recipe.json'
  }).as('listaReservasPendientes');
});

Given('una reserva en estado {string} y otra en estado {string} en el sistema', (_estado1: string, _estado2: string) => {
  cy.intercept('GET', '**/api/reservation/pending', {
    statusCode: 200,
    fixture: 'reservations-non-pending.json'
  }).as('listaReservasPendientes');
});

Then('la lista de pendientes muestra el mensaje {string}', (mensaje: string) => {
  cy.contains(mensaje).should('be.visible');
});

Given('una reserva pendiente que requiere receta médica pero no tiene receta adjunta', () => {
  cy.wrap({ codigo: 'RES-NO-UPLOAD', estado: 'Pending' }).as('reservaSeleccionada');
  cy.intercept('GET', '**/api/reservation/pending', {
    statusCode: 200,
    fixture: 'reservations-requires-recipe-no-upload.json'
  }).as('listaReservasPendientes');
});

Then('el boton {string} debe estar deshabilitado', (selector: string) => {
  cy.get(selector).should('be.disabled');
});
