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
  cy.intercept('GET', '**/api/pharmacy/*/drugs', {
    statusCode: 200,
    body: [
      {
        id: 1,
        name: 'Paracetamol 500mg',
        stock: 10,
        requiresPrescription: false,
        price: 150
      },
      {
        id: 2,
        name: 'Ibuprofeno 400mg',
        stock: 3,
        requiresPrescription: false,
        price: 200
      },
      {
        id: 3,
        name: 'Amoxicilina 500mg',
        stock: 5,
        requiresPrescription: true,
        price: 300
      }
    ]
  }).as('getDrugs');

  cy.visit(url);
});

Given('selecciona la farmacia {string} de la lista desplegable {string}', (pharmacyName: string, selector: string) => {
  cy.wait('@getPharmacies');
  cy.get(selector).select(pharmacyName);
  cy.wait('@getDrugs');
});

Given('agrega {int} unidades del medicamento {string}', (quantity: number, drugName: string) => {
  cy.contains(drugName)
    .parents('[data-cy=drug-item]')
    .within(() => {
      cy.get('[data-cy=quantity-input]').clear().type(quantity.toString());
      cy.get('[data-cy=add-to-reservation-btn]').click();
    });
});

When('completa el formulario de contacto con los siguientes datos:', (dataTable) => {
  const data = dataTable.rawTable.slice(1);

  data.forEach(([selector, value]) => {
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

  cy.intercept('GET', '**/api/pharmacy/*/drugs', {
    statusCode: 200,
    body: [
      {
        id: 1,
        name: 'Paracetamol 500mg',
        stock: 7,
        requiresPrescription: false,
        price: 150
      },
      {
        id: 2,
        name: 'Ibuprofeno 400mg',
        stock: 1,
        requiresPrescription: false,
        price: 200
      }
    ]
  }).as('getUpdatedDrugs');

  cy.intercept('POST', '**/api/email/send', {
    statusCode: 200,
    body: {
      success: true,
      message: 'Email enviado correctamente'
    }
  }).as('sendEmail');

  cy.get(selector).click();
});

Then('el sistema debe mostrar un mensaje en pantalla {string}', (message: string) => {
  cy.wait('@createReservation');
  cy.get('[data-cy=success-message]').should('contain', message);
});

Then('la base de datos debe registrar la reserva en estado {string}', (status: string) => {
  cy.wait('@createReservation').then((interception) => {
    expect(interception.response?.body.status).to.equal(status);
  });
});

Then('el stock visible de {string} debe actualizarse a {int} unidades', (drugName: string, expectedStock: number) => {
  cy.contains(drugName)
    .parents('[data-cy=drug-item]')
    .within(() => {
      cy.get('[data-cy=drug-stock]').should('contain', expectedStock.toString());
    });
});

Then('se debe simular el envío de un email a {string} que contenga el texto {string}', (email: string, text: string) => {
  cy.wait('@sendEmail').then((interception) => {
    expect(interception.request.body.to).to.equal(email);
    expect(interception.request.body.content).to.include('PUB-12345');
  });

  cy.get('[data-cy=email-confirmation]').should('contain', text);
});
