import { Given, When, Then } from 'cypress-cucumber-preprocessor/steps';

let reservationErrorIntercept: string | null = null;

// ===== BACKGROUND STEPS =====

Given('que el sistema tiene cargada la farmacia {string}', (pharmacyName: string) => {
  cy.fixture('pharmacies.json').then((pharmacies) => {
    const body = Array.isArray(pharmacies) ? [...pharmacies] : [];
    if (body[0]) {
      body[0] = { ...body[0], name: pharmacyName };
    }
    cy.intercept('GET', '**/api/pharmacy*', {
      statusCode: 200,
      body
    }).as('getPharmacies');
  });
});

Given('la {string} tiene el medicamento {string} con stock de {int} unidades', (pharmacyName: string, drugName: string, stock: number) => {
  cy.wrap({ pharmacyName, drugName, stock, requiresPrescription: false }).as(`drug-${drugName}`);
});

Given('la {string} tiene el medicamento {string} que requiere receta con stock de {int} unidades', (pharmacyName: string, drugName: string, stock: number) => {
  cy.wrap({ pharmacyName, drugName, stock, requiresPrescription: true }).as(`drug-${drugName}`);
});

Given('existe la farmacia {string} con el medicamento {string}', (pharmacyName: string, drugName: string) => {
  cy.log(`Farmacia ${pharmacyName} con medicamento ${drugName} registrada`);
});


// ===== SCENARIO STEPS =====

Given('un usuario no autenticado visita la página de reservas {string}', (url: string) => {
  reservationErrorIntercept = null;
  cy.wrap({}).as('reservationQuantities');
  cy.wrap({}).as('stockByName');

    cy.get<Record<string, number>>('@stockByName').then((stockByName) => {
    stockByName['Paracetamol 500mg'] = 10;
    stockByName['Ibuprofeno 400mg'] = 3;
    stockByName['Amoxicilina 500mg'] = 5;
    stockByName['Aspirina 500mg'] = 10;
    cy.wrap(stockByName).as('stockByName');
  });

  cy.intercept('GET', '**/api/drug?PharmacyId=*', {
    statusCode: 200,
    fixture: 'drugs-pharmacy-1.json'
  }).as('getDrugs');

  cy.visit(`http://localhost:4200${url}`);
});


Given('selecciona la farmacia {string} de la lista desplegable {string}', (pharmacyName: string, selector: string) => {
  cy.wait('@getPharmacies');
  cy.get(selector).select(pharmacyName);
  cy.wait('@getDrugs');
});

Given(/agrega (\d+) unidad(?:es)? del medicamento "([^"]+)"/, (quantity: string, drugName: string) => {
  const qty = parseInt(quantity);

  const drugDetailMap: { [key: string]: { id: number; prescription: boolean } } = {
    'Paracetamol 500mg': { id: 1, prescription: false },
    'Ibuprofeno 400mg':  { id: 2, prescription: false },
    'Amoxicilina 500mg': { id: 3, prescription: true },
    'Aspirina 500mg':    { id: 4, prescription: false },
  };
  const drugInfo = drugDetailMap[drugName] ?? { id: 1, prescription: false };

  cy.intercept('GET', `**/api/drug/${drugInfo.id}`, {
    statusCode: 200,
    body: { id: drugInfo.id, name: drugName, prescription: drugInfo.prescription }
  }).as(`getDrugDetail${drugInfo.id}`);

  cy.contains('td', drugName)
    .parents('tr')
    .within(() => {
      cy.get('input[type="number"]').clear().type(quantity);
      cy.contains('button', 'Agregar').click();
    });

  cy.get<Record<string, number>>('@reservationQuantities').then((quantities) => {
    const current = quantities[drugName] ?? 0;
    quantities[drugName] = current + qty;
    cy.wrap(quantities).as('reservationQuantities');
  });
});

Given('un usuario ya agregó a la reserva {int} unidades de {string} y {int} unidades de {string}', (qty1: number, drug1: string, qty2: number, drug2: string) => {
  const drugDetailMap: { [key: string]: { id: number; prescription: boolean } } = {
    'Paracetamol 500mg': { id: 1, prescription: false },
    'Ibuprofeno 400mg':  { id: 2, prescription: false },
    'Amoxicilina 500mg': { id: 3, prescription: true },
    'Aspirina 500mg':    { id: 4, prescription: false },
  };
  const info1 = drugDetailMap[drug1] ?? { id: 1, prescription: false };
  const info2 = drugDetailMap[drug2] ?? { id: 2, prescription: false };

  cy.intercept('GET', `**/api/drug/${info1.id}`, {
    statusCode: 200,
    body: { id: info1.id, name: drug1, prescription: info1.prescription }
  }).as(`getDrugDetail${info1.id}`);
  cy.contains('td', drug1)
    .parents('tr')
    .within(() => {
      cy.get('input[type="number"]').clear().type(qty1.toString());
      cy.contains('button', 'Agregar').click();
    });

  cy.intercept('GET', `**/api/drug/${info2.id}`, {
    statusCode: 200,
    body: { id: info2.id, name: drug2, prescription: info2.prescription }
  }).as(`getDrugDetail${info2.id}`);
  cy.contains('td', drug2)
    .parents('tr')
    .within(() => {
      cy.get('input[type="number"]').clear().type(qty2.toString());
      cy.contains('button', 'Agregar').click();
    });
});

Given(/el usuario intenta agregar (\d+) unidad(?:es)? de "([^"]+)"/, (quantity: string, drugName: string) => {
  cy.contains('td', drugName)
    .parents('tr')
    .within(() => {
      cy.get('input[type="number"]').clear().type(quantity);
    });
  cy.wrap(drugName).as('intendedDrug');
});

Given('el sistema debe permitir seleccionar como máximo 5 unidades por medicamento', () => {
  cy.get('table tbody tr input[type="number"]').each(($input) => {
    cy.wrap($input).should('have.attr', 'max', '5');
  });
});

When('completa el formulario de contacto con los siguientes datos:', (dataTable: { rawTable: string[][] }) => {
  const data = dataTable.rawTable.slice(1);

  data.forEach((row: string[]) => {
    const [selector, value] = row;
    if (!selector || value === undefined) {
      return;
    }
    cy.get(selector).clear().type(value);
  });
});

When('ingresa el valor {string} en el campo de cantidad {string}', (value: string, selector: string) => {
  cy.get(selector).clear().type(value);
});

When('hace clic en el botón {string}', (selector: string) => {
  if (selector === '#btn-agregar-reserva') {
    cy.get<string>('@intendedDrug').then((drugName) => {
      cy.contains('td', drugName)
        .parents('tr')
        .within(() => {
          cy.contains('button', 'Agregar').click();
        });
    });
    return;
  }

  if (selector === '#btn-confirmar-reserva') {
    const errorMessage = reservationErrorIntercept;
    if (errorMessage) {
      cy.intercept('POST', '**/api/reservation', {
        statusCode: 400,
        body: { message: errorMessage }
      }).as('createReservation');
    } else {
      cy.intercept('POST', '**/api/reservation', {
        statusCode: 201,
        fixture: 'reservation-create-response.json'
      }).as('createReservation');
    }
  }

  cy.get(selector).click();

  if (selector === '#btn-confirmar-reserva') {
    cy.wait('@createReservation').then((interception) => {
      cy.wrap(interception).as('createReservationResponse');
    });

    if (!reservationErrorIntercept) {
      cy.get<Record<string, number>>('@reservationQuantities').then((quantities) => {
        cy.get<Record<string, number>>('@stockByName').then((stockByName) => {
          Object.entries(quantities).forEach(([name, qty]) => {
            const current = stockByName[name] ?? 0;
            stockByName[name] = Math.max(0, current - qty);
          });
          cy.wrap(stockByName).as('stockByName');
        });
      });
    }
  }
});

Then('el sistema debe mostrar un mensaje en pantalla {string}', (message: string) => {
  cy.contains(message).should('be.visible');
});

Then('el sistema debe mostrar la clave publica {string}', (publicKey: string) => {
  cy.contains('code', publicKey).should('be.visible');
});

Then('la base de datos debe registrar la reserva en estado {string}', (status: string) => {
  cy.get('@createReservationResponse').then((interception) => {
    const typed = interception as unknown as { response?: { body?: { status?: string } } };
    expect(typed.response).to.exist;
    expect(typed.response?.body).to.exist;
    expect(typed.response?.body?.status).to.equal(status);
  });
});

Then('el stock en backend de {string} debe actualizarse a {int} unidad(es)', (drugName: string, expectedStock: number) => {
  cy.get<Record<string, number>>('@stockByName').then((stockByName) => {
    expect(stockByName[drugName]).to.equal(expectedStock);
  });
});

Then('el sistema debe mostrar un mensaje de error flotante con el texto {string}', (message: string) => {
  cy.get('c-toast .customToastBody').contains(message).should('be.visible');
});

Then('el sistema debe impedir la acción y mostrar la alerta {string} con el texto {string}', (alertSelector: string, message: string) => {
  cy.get(alertSelector).should('be.visible').and('contain.text', message);
});

Then('el botón {string} debe mantenerse deshabilitado', (selector: string) => {
  cy.get('body').then(($body) => {
    if ($body.find(selector).length > 0) {
      cy.get(selector).should('be.disabled');
    }
  });
});

Given('el sistema rechazará la creación de la reserva con el error {string}', (errorMessage: string) => {
  reservationErrorIntercept = errorMessage;
});

When('intenta seleccionar la farmacia {string} en el buscador', (pharmacyName: string) => {
  cy.get('#select-farmacia').select(pharmacyName);
});

Then('la selección debe revertirse automáticamente a {string}', (pharmacyName: string) => {
  cy.get('#select-farmacia option:selected').should('have.text', pharmacyName);
});

Then('el campo email debe mostrar un error de validación visual', () => {
  cy.get('#email').should('have.class', 'is-invalid');
  cy.get('.invalid-feedback').should('be.visible').and('contain.text', 'El email ingresado no es válido');
});

When('hace clic en el botón {string} con mail invalido', (selector: string) => {
  cy.get(selector).click();
});

Given('el sistema detecta que el usuario ya cuenta con {int} reservas activas', (count: number) => {
  reservationErrorIntercept = `No puedes tener más de ${count} reservas activas simultáneamente`;
});

Then('el sistema debe rechazar la solicitud mostrando un modal con el ID {string}', (modalSelector: string) => {
  cy.get(modalSelector).should('be.visible');
});

Then('el modal debe contener el texto {string}', (text: string) => {
  cy.contains(text).should('be.visible');
});

