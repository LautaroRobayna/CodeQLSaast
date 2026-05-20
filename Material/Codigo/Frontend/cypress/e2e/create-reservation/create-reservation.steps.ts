import { Given, When, Then } from 'cypress-cucumber-preprocessor/steps';

// ===== BACKGROUND STEPS =====

Given('que el sistema tiene cargada la farmacia {string}', (pharmacyName: string) => {
  cy.intercept('GET', '**/api/pharmacy*', {
    statusCode: 200,
    body: [
      {
        id: 1,
        name: pharmacyName,
        address: 'Av. Principal 123'
      },
      {
        id: 2,
        name: 'Farmacia Norte',
        address: 'Calle Norte 456'
      }
    ]
  }).as('getPharmacies');
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

Given('esta logueado con email {string} y contrasenia {string}', (email: string, _password: string) => {
  cy.intercept('POST', '**/api/auth/login', {
    statusCode: 200,
    body: {
      token: 'fake-jwt-token',
      user: {
        email: email,
        name: 'Carlos Gómez'
      }
    }
  }).as('login');

  localStorage.setItem('authToken', 'fake-jwt-token');
  localStorage.setItem('userEmail', email);
});

// ===== SCENARIO STEPS =====

Given('un usuario no autenticado visita la página de reservas {string}', (url: string) => {
  cy.wrap({}).as('reservationQuantities');
  cy.wrap({}).as('stockByName');

    cy.get<Record<string, number>>('@stockByName').then((stockByName) => {
    stockByName['Paracetamol 500mg'] = 10;
    stockByName['Ibuprofeno 400mg'] = 3;
    stockByName['Amoxicilina 500mg'] = 5;
    cy.wrap(stockByName).as('stockByName');
  });

  cy.intercept('GET', '**/api/drug?PharmacyId=*', {
    statusCode: 200,
    body: [
      {
        id: 1,
        code: 'P-500',
        name: 'Paracetamol 500mg',
        symptom: 'Dolor',
        price: 150
      },
      {
        id: 2,
        code: 'I-400',
        name: 'Ibuprofeno 400mg',
        symptom: 'Fiebre',
        price: 200
      },
      {
        id: 3,
        code: 'A-500',
        name: 'Amoxicilina 500mg',
        symptom: 'Infeccion',
        price: 300
      }
    ]
  }).as('getDrugs');

  cy.visit(`http://localhost:4200${url}`);
});

Given('selecciona la farmacia {string} de la lista desplegable {string}', (pharmacyName: string, selector: string) => {
  cy.wait('@getPharmacies');
  cy.get(selector).select(pharmacyName);
  cy.wait('@getDrugs');
});

Given('agrega {int} unidades del medicamento {string}', (quantity: number, drugName: string) => {
  cy.contains('td', drugName)
    .parents('tr')
    .within(() => {
      cy.get('input[type="number"]').clear().type(quantity.toString());
      cy.contains('button', 'Agregar').click();
    });

  cy.get<Record<string, number>>('@reservationQuantities').then((quantities) => {
    quantities[drugName] = quantity;
    cy.wrap(quantities).as('reservationQuantities');
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

When('hace clic en el botón {string}', (selector: string) => {
  cy.intercept('POST', '**/api/reservation', {
    statusCode: 201,
    body: {
      id: 123,
      status: 'Pendiente',
      publicKey: 'PUB-12345',
      items: [
        { drugName: 'Paracetamol 500mg', quantity: 3 },
        { drugName: 'Ibuprofeno 400mg', quantity: 2 }
      ]
    }
  }).as('createReservation');

  cy.get(selector).click();

  cy.wait('@createReservation').then((interception) => {
    cy.wrap(interception).as('createReservationResponse');
  });

  cy.get<Record<string, number>>('@reservationQuantities').then((quantities) => {
    cy.get<Record<string, number>>('@stockByName').then((stockByName) => {
      Object.entries(quantities).forEach(([name, qty]) => {
        const current = stockByName[name] ?? 0;
        stockByName[name] = Math.max(0, current - qty);
      });

      cy.wrap(stockByName).as('stockByName');
    });
  });
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

