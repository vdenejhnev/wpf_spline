#include "pchh.h"
#include "pch.h"
#include "mkl.h"
#include <iostream>

struct data
{
	double* uniform = NULL;
	double* X = NULL;
	double* Y = NULL;
};

void Generate(MKL_INT* X, MKL_INT* nX, double* Y, double* Ynev, void* usr_data)
{
	data* d = (data*)usr_data;
	CubicSpline(*nX, d->X, 1, Y, *X, d->X[0], d->X[*nX - 1], Ynev);

	for (int i = 0; i < *X; i++)
	{
		Ynev[i] -= d->Y[i];
	}

}
extern "C" _declspec(dllexport)
bool TrustRegion(MKL_INT n, MKL_INT m, double* x, double* y, const double* eps, double jac_eps, MKL_INT niter1, MKL_INT niter2, double rs, MKL_INT& ndoneIter, double& resInitial, double& resFinal, MKL_INT& stopCriteria, MKL_INT* checkInfo, int& error)

{
	_TRNSP_HANDLE_t handle = NULL;
	double* fvec = NULL;
	double* fjac = NULL;
	error = 0;
	try
	{
		fvec = new double[m];
		fjac = new double[n * m];
		double* Yitr = new double[m];
		for (int i = 0; i < m - 1; i++)
		{
			Yitr[i] = 3;
		}

		double* Xravnomer = new double[m];
		for (int i = 0; i < m - 1; i++)
		{
			Xravnomer[i] = x[0] + ((x[n - 1] - x[0]) / m) * i;
		}
		data* dataopt = NULL;
		dataopt->uniform = Xravnomer;
		dataopt->X = x;
		dataopt->Y = y;
		MKL_INT ret = dtrnlsp_init(&handle, &m, &n, Yitr, eps, &niter1, &niter2, &rs);
		if (ret != TR_SUCCESS) throw (-1);
		ret = dtrnlsp_check(&handle, &m, &n, fjac, fvec, eps, checkInfo);
		if (ret != TR_SUCCESS) throw (1);
		MKL_INT RCI_Request = 0; 
		while (true)
		{
			ret = dtrnlsp_solve(&handle, fvec, fjac, &RCI_Request);
			if (ret != TR_SUCCESS) throw (2);
			if (RCI_Request == 0) continue;
			else if (RCI_Request == 1) Generate(&m, &n, x, fvec, dataopt);
			else if (RCI_Request == 2)
			{
				ret = djacobix(Generate, &n, &m, fjac, x, &jac_eps, dataopt);
				if (ret != TR_SUCCESS) throw (3);
			}
			else if (RCI_Request >= -6 && RCI_Request <= -1) break;
			else throw (4);
		}
			ret = dtrnlsp_get(&handle, &ndoneIter, &stopCriteria,
				&resInitial, &resFinal);
		if (ret != TR_SUCCESS) throw (5);
		ret = dtrnlsp_delete(&handle);
		if (ret != TR_SUCCESS) throw (6);
	}
	catch (int _error) { error = _error; }
	if (fvec != NULL) delete[] fvec;
	if (fjac != NULL) delete[] fjac;
}